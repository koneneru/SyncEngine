using YandexDisk.Client;
using SyncEngine.ServerProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Protocol;

namespace SyncEngine.Source_Files.ServerProviders.YandexServerProvider
{
	internal class YandexServerProvider : IServerProvider
	{
		private IDiskApi diskApi;

		public string ConnectionString => throw new NotImplementedException();

		public bool IsConnected => throw new NotImplementedException();

		public ServerProviderStatus Status => throw new NotImplementedException();

		public SyncContext SyncContext { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public List<FileBasicInfo> FileList => throw new NotImplementedException();

		public event EventHandler<ServerProviderStateChangedEventArgs> ServerProviderStateChanged;

		public Task<Result> Connect()
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public Task<Result> DownloadFileAsync(string path, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task DownloadFileAsync(string path, Stream stream, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public async Task<DataResult<List<FileBasicInfo>>> GetFileList(string subDir, CancellationToken cancellationToken)
		{
			var r = new FilesResourceRequest()
			{
				Offset = 0,
				Limit = 0,
				MediaType = new []{ MediaType.Image },
			};

			var info = await diskApi.MetaInfo.GetFilesInfoAsync(r, cancellationToken);

			throw new NotImplementedException();
		}

		public Task GetFileListAsync(string subDir, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public IServerFileReader GetFileReader()
		{
			throw new NotImplementedException();
		}

		public Task<FileBasicInfo?> GetPlaceholderAsync(string relativePath)
		{
			throw new NotImplementedException();
		}

		public Task<Result> RemoveAsync(string relativePath)
		{
			throw new NotImplementedException();
		}

		public async Task<Result> UploadFileAsync(string path, Stream fileStream, UploadMode uploadMode, CancellationToken cancellationToken)
		{
			bool overwrite = uploadMode == UploadMode.Create;

			await diskApi.Files.UploadFileAsync(path, overwrite, fileStream, cancellationToken);

			throw new NotImplementedException();
		}
	}
}
