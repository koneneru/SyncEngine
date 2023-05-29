using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Storage.Provider;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine.ServerProviders
{
	public class LocalServerProvider : IServerProvider
	{
		public SyncContext syncContext;

		private readonly string localServerPath;
		private ServerProviderStatus status = ServerProviderStatus.Disconnected;
		private readonly System.Threading.Timer connectionTimer;
		private readonly System.Threading.Timer fullResyncTimer;
		private Stream? fileStream;

		public event EventHandler<ServerProviderStateChangedEventArgs> ServerProviderStateChanged;
		public event EventHandler<FileChangedEventArgs> FileChanged;

		public Dictionary<string, FileBasicInfo> fileList = new();

		// Since this is a local disk to local-disk copy, it would happen really fast.
		// This is the size of each chunk to be copied due to the overlapped approach.
		// I pulled this number out of a hat.
		private static readonly int chunkSize = 4096;
		// Arbitrary delay per chunk, again, so you can actually see the progress bar
		// move.
		private readonly int chunkDelayms = 250;

		private bool inSyncing = false;

		public string ConnectionString { get { return localServerPath; } }

		public ServerProviderStatus Status { get { return status; } }

		public SyncContext SyncContext { get { return syncContext; } set { syncContext = value; } }

		public bool IsConnected { get { return Status == ServerProviderStatus.Connected; } }

		public Dictionary<string, FileBasicInfo> FileList { get { return fileList; } }

        public LocalServerProvider(string path)
        {
			localServerPath = path;
			connectionTimer = new(ConnectionTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
			fullResyncTimer = new(FullResyncTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

		private void ConnectionTimerCallback(object? state)
		{
			CheckProviderStatus();
		}

		private void FullResyncTimerCallback(object? state)
		{
			RaiseFileChanged(new() { ChangeType = WatcherChangeTypes.All, ResyncSubDirectories = true });
		}

        public Task<Result> Connect()
		{
			ChangeStatus(ServerProviderStatus.Connecting);

			Result result;

			if (Directory.Exists(localServerPath))
			{
				ChangeStatus(ServerProviderStatus.Connected);
				result = new();
			}
			else
			{
				ChangeStatus(ServerProviderStatus.Failed);
				result = new(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE);
			}

			return Task.FromResult(result);
		}

		public Task<Result> Disconnect()
		{
			ChangeStatus(ServerProviderStatus.Disconnected);
			return Task.FromResult(new Result());
		}

		public IServerFileReader GetFileReader()
		{
			return new ServerFileReader(this);
		}

		public async Task<DataResult<List<FileBasicInfo>>> GetFileList(string subDir, CancellationToken cancellationToken)
		{
			if (status != ServerProviderStatus.Connected)
				return new DataResult<List<FileBasicInfo>>(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE);

			List<FileBasicInfo> placeholders = new();

			using (var filesInfoList = new LocalServerFilesInfoList(this))
			{
				var fillListResult = await filesInfoList.FillListAsync(subDir, cancellationToken);

				if (!fillListResult.Succeeded)
					return new DataResult<List<FileBasicInfo>>(fillListResult.Status);

				var takeNextResult = filesInfoList.TakeNext();
				do
				{
					if (takeNextResult.Data != null)
						placeholders.Add(takeNextResult.Data);

					if (cancellationToken.IsCancellationRequested)
						break;

					takeNextResult = filesInfoList.TakeNext();
				}
				while (takeNextResult.Succeeded && !cancellationToken.IsCancellationRequested);
			}

			return new DataResult<List<FileBasicInfo>>(placeholders);
		}

		public Task<FileBasicInfo?> GetPlaceholderAsync(string relativePath)
		{
			return Task.Run(() => GetPlaceholder(relativePath));
		}

		private FileBasicInfo? GetPlaceholder(string path)
		{
			string fullPath = Path.Combine(localServerPath, path);
			if (File.Exists(fullPath))
			{
				FileInfo fileInfo = new(fullPath);
				return new FileBasicInfo(fullPath, fileInfo);
			}
			else 
				return default;
		}

		private void ChangeStatus(ServerProviderStatus status)
		{
			if(status != this.status)
			{
				this.status = status;
				if(status == ServerProviderStatus.Connected)
				{
					// Ful; Sync after reconnect, then every 2 hours
					fullResyncTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(120));

					// Check existing connection every 60 seconds
					connectionTimer.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
				}
				else
				{
					// Disable full resync ifnot connected.
					fullResyncTimer.Change(Timeout.Infinite, Timeout.Infinite);
				}

				if(status == ServerProviderStatus.Disconnected)
				{
					connectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
				}

				RaiseServerProviderStateChanged(new ServerProviderStateChangedEventArgs(status));
			}
		}

		private bool CheckProviderStatus() // Maybe should be deleted
		{
			if (this.status == ServerProviderStatus.Disabled) return false;

			bool isOnline = Directory.Exists(localServerPath);
			ChangeStatus(isOnline ? ServerProviderStatus.Connected : ServerProviderStatus.Disconnected);
			return isOnline;
		}

		public Task<DataResult<FileBasicInfo>> CreateDirectoryAsync(string path)
		{
			DataResult<FileBasicInfo> result;

			string fullPath = Path.Combine(localServerPath, path);

			if (!Directory.Exists(fullPath))
			{
				Directory.CreateDirectory(fullPath);
				result = new(new FileBasicInfo(fullPath, new DirectoryInfo(fullPath)));
			}
			else
			{
				result = new(NtStatus.STATUS_UNSUCCESSFUL);
			}

			return Task.FromResult(result);
		}

		public async Task DownloadFileAsync(string path, Stream stream, CancellationToken cancellationToken)
		{
			await Task.Run(() => DownloadFile(path, stream), cancellationToken);
		}

		private static void DownloadFile(string path, Stream fileStream)
		{
			using (FileStream remoteFS = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				var total = (ulong)fileStream.Length;
				ulong current = 0;
				var buffer = new byte[chunkSize];
				int readBytes = remoteFS.Read(buffer, 0, chunkSize);
				while (readBytes > 0)
				{
					fileStream.Write(buffer, 0, readBytes);
					current += (ulong)readBytes;
					//progress.UpdateProgress(current, total);
					readBytes = remoteFS.Read(buffer, 0, chunkSize);
				}

				fileStream.Dispose();
			}
		}

		public async Task<Result> UploadFileAsync(string path, Stream fileStream, UploadMode uploadMode, CancellationToken cancellationToken)
		{
			if (!IsConnected)
				return new Result(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE);

			Result result;

			try
			{
				string fullPath = Path.Combine(localServerPath, path);
				if (uploadMode != UploadMode.Create)
				{
					fullPath = Path.Combine(localServerPath, "$_", Path.GetFileName(path));
				}

				FileMode fileMode = uploadMode switch
				{
					UploadMode.Create => FileMode.Create,
					UploadMode.Update => FileMode.Open,
					UploadMode.Resume => FileMode.Open,
					_ => FileMode.OpenOrCreate,
				};

				await Task.Run(()=> UploadFile(fullPath, fileStream, fileMode), cancellationToken);

				result = new(NtStatus.STATUS_SUCCESS);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed Upload File {path}: {ex.Message}");
				result = new(ex);
			}

			await GetFileListAsync(string.Empty, cancellationToken);

			return result;
		}

		private static void UploadFile(string path, Stream fileStream, FileMode fileMode)
		{
			using (FileStream remoteFS = new(path, fileMode, FileAccess.Write, FileShare.None))
			{
				var total = (ulong)fileStream.Length;
				ulong current = 0;
				var buffer = new byte[chunkSize];
				int readBytes = fileStream.Read(buffer, 0, chunkSize);
				while (readBytes > 0)
				{
					remoteFS.Write(buffer, 0, readBytes);
					current += (ulong)readBytes;
					//progress.UpdateProgress(current, total);
					readBytes = fileStream.Read(buffer, 0, chunkSize);
				}

				fileStream.Dispose();
			}
		}

		private void RaiseFileChanged(FileChangedEventArgs e)
		{
			try
			{
				FileChanged?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed raise FileChangedEvent on LocalServerProvider, 0x{ex.HResult:X8}");
            }
		}

		private void RaiseServerProviderStateChanged(ServerProviderStateChangedEventArgs e)
		{
			ServerProviderStateChanged?.Invoke(this, e);
		}

		public async Task GetFileListAsync(string subDir, CancellationToken cancellationToken)
		{
			if (!inSyncing)
			{
				inSyncing = true;
				fileList.Clear();
				await Task.Run(() => GetFileListRecursive(subDir), cancellationToken);
				inSyncing = false;
			}
			
		}

		private void GetFileListRecursive(string subDir)
		{
			string fullPath = Path.Combine(localServerPath, subDir);

			var directory = new DirectoryInfo(fullPath);
			var t = directory.EnumerateFileSystemInfos();
			foreach (var item in t)
			{
				string relativePath = Path.GetRelativePath(localServerPath, item.FullName);
				fileList.Add(relativePath, new FileBasicInfo(localServerPath, item));

				if (item.Attributes.HasFlag(FileAttributes.Directory))
				{
					GetFileListRecursive(relativePath);
				}
			}
		}

		public Task<Result> RemoveAsync(string relativePath)
		{
			Result result;

			string fullPath = Path.Combine(localServerPath, relativePath);

			try
			{
				if(File.Exists(fullPath)) File.Delete(fullPath);
				else Directory.Delete(fullPath);

				fileList.Remove(relativePath);

				result = new();
			}
			catch(Exception ex)
			{
				result = new(ex);
			}

			return Task.FromResult(result);
		}

		public IDownloader CreateDownloader() { return new LocalDownloader(this); }
	}
}
