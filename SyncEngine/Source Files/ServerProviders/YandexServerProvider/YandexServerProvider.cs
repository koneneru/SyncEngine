using YandexDisk.Client;
using SyncEngine.ServerProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Protocol;
using System.Collections.Concurrent;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;
using System.Runtime.InteropServices;

namespace SyncEngine.ServerProviders
{
	public class YandexServerProvider : IServerProvider
	{
		//#error define your client_id and return_url
		private const string CLIENT_ID = "24b335c9dc014ffb9fce8beee6afa271";
		private const string RETURN_URL = "localhost:12345/callback";

		private IDiskApi diskApi;
		//private IDiskSdkClient sdk;
		private readonly string accessToken;
		private ServerProviderStatus status = ServerProviderStatus.Disabled;
		private readonly System.Threading.Timer connectionTimer;

		private Task syncingTask = Task.Delay(5000);
		public Task SyncingTask => syncingTask;

		private ConcurrentDictionary<string, FileBasicInfo> fileList = new();

		public static string ClientId => CLIENT_ID;

		public bool IsConnected => Status == ServerProviderStatus.Connected;

		public string Token => accessToken;
		public string ConnectionString => accessToken;

		public ServerProviderStatus Status => status;

		public SyncContext SyncContext { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public ConcurrentDictionary<string, FileBasicInfo> FileList => fileList;

		public event EventHandler<ServerProviderStateChangedEventArgs> ServerProviderStateChanged;

        public YandexServerProvider(string token)
        {
			accessToken = token;
			diskApi = new DiskHttpApi(Token);

			connectionTimer = new(ConnectionTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
		}

		public Task<Result> Connect()
		{
			ChangeStatus(ServerProviderStatus.Connecting);
			ChangeStatus(ServerProviderStatus.Connected);

			return Task.FromResult(new Result());
		}

		private void ConnectionTimerCallback(object? state)
		{

		}

		private void ChangeStatus(ServerProviderStatus status)
		{
			if (status != this.status)
			{
				this.status = status;
				if (status == ServerProviderStatus.Connected)
				{
					// Check existing connection every 60 seconds
					connectionTimer.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
				}

				if (status == ServerProviderStatus.Disconnected)
				{
					connectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
				}

				RaiseServerProviderStateChanged(new ServerProviderStateChangedEventArgs(status));
			}
		}

		public Task<DataResult<FileBasicInfo>> CreateDirectoryAsync(string path)
		{
			throw new NotImplementedException();
		}

		public IDownloader CreateDownloader()
		{
			throw new NotImplementedException();
		}

		public Task<Result> Disconnect()
		{
			ChangeStatus(ServerProviderStatus.Disconnected);
			return Task.FromResult(new Result());
		}

		public async Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken)
		{
			path = path.Replace("\\", "/");
			return await diskApi.Files.DownloadFileAsync(path, cancellationToken);
		}

		public Task DownloadFileAsync(string path, Stream stream, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public async Task GetFileListAsync(string subDir, CancellationToken cancellationToken)
		{
			var r = new FilesResourceRequest()
			{
				Offset = 0,
				Limit = 100,
				//MediaType = new MediaType[] { MediaType.Audio, MediaType.Backup, MediaType.Book, MediaType.Compressed,
				//	MediaType.Data, MediaType.Development, MediaType.Diskimage, MediaType.Document, MediaType.Encoded,
				//	MediaType.Executable, MediaType.Flash, MediaType.Font, MediaType.Image, MediaType.Settings,
				//	MediaType.Spreadsheet, MediaType.Text, MediaType.Unknown, MediaType.Video, MediaType.Web }
			};

			var info = await diskApi.MetaInfo.GetFilesInfoAsync(r, cancellationToken);

			fileList.Clear();
			foreach(var item in info.Items)
			{
				string relativePath = item.Path[6..];
				relativePath = relativePath.Replace('/', '\\');
				string dirPath = string.Empty;
				int offset = 0;

				while(relativePath[offset..] != item.Name)
				{
					var dirName = relativePath[offset..];
					dirName = dirName[..dirName.IndexOf('\\')];
					dirPath = Path.Combine(dirPath, dirName);
					offset += dirPath.Length + 1;

					FileBasicInfo dirInfo = new()
					{
						RelativePath = dirPath,
						RelativeFileName = dirName,
						FileIdentity = Marshal.StringToCoTaskMemUni(dirPath),
						FileIdentityLength = (uint)(dirPath.Length * Marshal.SizeOf(dirPath[0])),
						FileSize = 0,
						FileAttributes = FileAttributes.Directory,
						CreationTime = DateTime.UtcNow,
						LastAccessTime = DateTime.UtcNow,
						LastWriteTime = DateTime.UtcNow,
						ChangeTime = DateTime.UtcNow,
						ETag = new StringBuilder('_')
							.Append(item.Modified.ToUniversalTime().Ticks)
							.Append('_')
							.Append(item.Size).ToString()
					};

					fileList.AddOrUpdate(dirPath, dirInfo, (key, value) => dirInfo);
				}

				FileBasicInfo basicInfo = new()
				{
					RelativePath = relativePath,
					RelativeFileName = item.Name,
					FileIdentity = Marshal.StringToCoTaskMemUni(relativePath),
					FileIdentityLength = (uint)(relativePath.Length * Marshal.SizeOf(relativePath[0])),
					FileSize = item.Size,
					FileAttributes = item.Type == ResourceType.Dir ? FileAttributes.Directory : FileAttributes.Normal,
					CreationTime = item.Created,
					LastAccessTime = item.Modified,
					LastWriteTime = item.Modified,
					ChangeTime = item.Modified,
					ETag = new StringBuilder('_')
							.Append(item.Modified.ToUniversalTime().Ticks)
							.Append('_')
							.Append(item.Size).ToString()
				};

				fileList.AddOrUpdate(relativePath, basicInfo, (key, value) => basicInfo);
			}
		}

		//public async Task<FileBasicInfo?> GetPlaceholderAsync(string relativePath, CancellationToken cancellationToken)
		//{
		//	throw new NotImplementedException();
		//}

		private void RaiseServerProviderStateChanged(ServerProviderStateChangedEventArgs e)
		{
			ServerProviderStateChanged?.Invoke(this, e);
		}

		public async Task<Result> RemoveAsync(string relativePath, CancellationToken cancellationToken)
		{
			relativePath = relativePath.Replace('\\', '/');
			DeleteFileRequest request = new()
			{
				Path = relativePath
			};
			_ = await diskApi.Commands.DeleteAsync(request, cancellationToken);

			return new Result();
		}

		public async Task<Result> UploadFileAsync(string path, Stream fileStream, UploadMode uploadMode, CancellationToken cancellationToken)
		{
			await diskApi.Files.UploadFileAsync(path.Replace('\\', '/'), true, fileStream, cancellationToken);

			//await GetFileListAsync(string.Empty, cancellationToken);
			await UpdateFileInfo(path, cancellationToken);

			return new Result();

			throw new NotImplementedException();
		}

		private async Task UpdateFileInfo(string path, CancellationToken cancellationToken)
		{
			ResourceRequest request = new()
			{
				Limit = 1,
				Path = path
			};

			var resource = await diskApi.MetaInfo.GetInfoAsync(request, cancellationToken);

			FileBasicInfo basicInfo = new()
			{
				RelativePath = path,
				RelativeFileName = resource.Name,
				FileIdentity = Marshal.StringToCoTaskMemUni(path),
				FileIdentityLength = (uint)(path.Length * Marshal.SizeOf(path[0])),
				FileSize = resource.Size,
				FileAttributes = resource.Type == ResourceType.Dir ? FileAttributes.Directory : FileAttributes.Normal,
				CreationTime = resource.Created,
				LastAccessTime = resource.Modified,
				LastWriteTime = resource.Modified,
				ChangeTime = resource.Modified,
				ETag = new StringBuilder('_')
						.Append(resource.Modified.ToUniversalTime().Ticks)
						.Append('_')
						.Append(resource.Size).ToString()
			};

			fileList.AddOrUpdate(path, basicInfo, (key, value) => basicInfo);
		}
	}
}
