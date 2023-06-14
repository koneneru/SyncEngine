using System;
using System.Collections.Concurrent;
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

		private string localServerPath;
		private ServerProviderStatus status = ServerProviderStatus.Disconnected;
		private readonly System.Threading.Timer connectionTimer;

		public event EventHandler<ServerProviderStateChangedEventArgs> ServerProviderStateChanged;

		public ConcurrentDictionary<string, FileBasicInfo> fileList = new();

		private Task syncingTask;
		public Task SyncingTask => syncingTask;

		// Since this is a local disk to local-disk copy, it would happen really fast.
		// This is the size of each chunk to be copied due to the overlapped approach.
		// I pulled this number out of a hat.
		private static readonly int chunkSize = 4096;
		// Arbitrary delay per chunk, again, so you can actually see the progress bar
		// move.
		private readonly int chunkDelayms = 250;

		//private bool inSyncing = false;

		public ServerProviderStatus Status => status;

		public bool IsConnected => Status == ServerProviderStatus.Connected;

		public ConcurrentDictionary<string, FileBasicInfo> FileList  => fileList;

		public string Token => localServerPath;
		public string ConnectionString => localServerPath;

		public LocalServerProvider(string token)
        {
			localServerPath = token;

			syncingTask = Task.Delay(100);

			connectionTimer = new(ConnectionTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

		private void ConnectionTimerCallback(object? state)
		{
			
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

		//public Task<FileBasicInfo?> GetPlaceholderAsync(string relativePath)
		//{
		//	return Task.Run(() => GetPlaceholder(relativePath));
		//}

		//private FileBasicInfo? GetPlaceholder(string path)
		//{
		//	string fullPath = Path.Combine(localServerPath, path);
		//	if (File.Exists(fullPath))
		//	{
		//		FileInfo fileInfo = new(fullPath);
		//		return new FileBasicInfo(fullPath, fileInfo);
		//	}
		//	else 
		//		return default;
		//}

		private void ChangeStatus(ServerProviderStatus status)
		{
			if(status != this.status)
			{
				this.status = status;
				if(status == ServerProviderStatus.Connected)
				{
					// Check existing connection every 60 seconds
					connectionTimer.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
				}

				if(status == ServerProviderStatus.Disconnected)
				{
					connectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
				}

				RaiseServerProviderStateChanged(new ServerProviderStateChangedEventArgs(status));
			}
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

		public async Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken)
		{
			string fullPath = Path.Combine(Token, path);
			return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
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
				//if (uploadMode != UploadMode.Create)
				//{
				//	fullPath = Path.Combine(localServerPath, "$_" + Path.GetFileName(path));
				//}

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

			//await GetFileListAsync(string.Empty, cancellationToken);

			UpdateFileList(path);

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

		private void UpdateFileList(string relativePath)
		{
			string fullPath = Path.Combine(localServerPath, relativePath);
			FileInfo fileInfo = new(fullPath);
			var newFileInfo = new FileBasicInfo(relativePath, fileInfo);
			fileList.AddOrUpdate(relativePath, newFileInfo, (key, value) => newFileInfo);

			//if (fileList.ContainsKey(relativePath)) fileList[relativePath] = new FileBasicInfo(relativePath, fileInfo);
			//else fileList.Add(relativePath, new FileBasicInfo(relativePath, fileInfo));
		}

		private void RaiseServerProviderStateChanged(ServerProviderStateChangedEventArgs e)
		{
			ServerProviderStateChanged?.Invoke(this, e);
		}

		public async Task GetFileListAsync(string subDir, CancellationToken cancellationToken)
		{
			if (syncingTask == null || syncingTask.IsCompleted)
			{
				fileList.Clear();
				syncingTask = Task.Run(() => GetFileListRecursive(subDir), cancellationToken);
				await syncingTask;
			}

			//if (!inSyncing)
			//{
			//	inSyncing = true;
			//	fileList.Clear();
			//	await Task.Run(() => GetFileListRecursive(subDir), cancellationToken);
			//	inSyncing = false;
			//}			
		}

		private void GetFileListRecursive(string subDir)
		{
			string fullPath = Path.Combine(localServerPath, subDir);

			var directory = new DirectoryInfo(fullPath);
			foreach (var item in directory.EnumerateFileSystemInfos())
			{
				string relativePath = Path.GetRelativePath(localServerPath, item.FullName);

				var newFileInfo = new FileBasicInfo(localServerPath, item);
				fileList.AddOrUpdate(relativePath, newFileInfo, (key, value) => value);

				//fileList.Add(relativePath, new FileBasicInfo(localServerPath, item));

				if (item.Attributes.HasFlag(FileAttributes.Directory))
				{
					GetFileListRecursive(relativePath);
				}
			}
		}

		public Task<Result> RemoveAsync(string relativePath, CancellationToken cancellationToken)
		{
			Result result;

			//if(fileList.Remove(relativePath))
			if(fileList.Remove(relativePath, out _))
			{
				string fullPath = Path.Combine(localServerPath, relativePath);

				try
				{
					if (File.Exists(fullPath)) File.Delete(fullPath);
					else Directory.Delete(fullPath, true);

					//fileList.Remove(relativePath);

					result = new();
				}
				catch (Exception ex)
				{
					result = new(ex);
				}
			}
			else
			{
				result = new(NtStatus.STATUS_UNSUCCESSFUL);
			}

			fileList.Remove(relativePath, out _);
			//fileList.Remove(relativePath);

			return Task.FromResult(result);
		}

		public IDownloader CreateDownloader() { return new LocalDownloader(this); }
	}
}
