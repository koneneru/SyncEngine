using SyncEngine.ServerProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public interface IServerProvider
	{
		public event EventHandler<ServerProviderStateChangedEventArgs> ServerProviderStateChanged;

		public string ConnectionString { get; }
		public bool IsConnected { get; }

		public ServerProviderStatus Status { get; }

		public SyncContext SyncContext { get; set; }

		public Dictionary<string, FileBasicInfo> FileList { get; }

		/// <summary>
		/// Establishing a connection to the Server to check Authentication and for receiving realtime updates.
		/// ServerProvider is responsible for authentication, reconnect, timeout handling etc.
		/// </summary>
		/// <returns>If function succeeds, it returns NTStatus.STATUS_SUCCESS. Otherwise, it returns NTStatus.STATUS_UNSUCCESSFUL</returns>
		public Task<Result> Connect();

		/// <summary>
		/// Disconnects a Server and stops recievingrealtime updates. 
		/// </summary>
		/// <returns>If function succeeds, it returns NTStatus.STATUS_SUCCESS. Otherwise, it returns NTStatus.STATUS_UNSUCCESSFUL</returns>
		public Task<Result> Disconnect();

		public IServerFileReader GetFileReader();

		public Task<FileBasicInfo?> GetPlaceholderAsync(string relativePath);

		public Task<DataResult<FileBasicInfo>> CreateDirectoryAsync(string path);

		public Task DownloadFileAsync(string path, Stream stream, CancellationToken cancellationToken);

		public Task<Result> UploadFileAsync(string path, Stream fileStream, UploadMode uploadMode, CancellationToken cancellationToken);

		public Task<DataResult<List<FileBasicInfo>>> GetFileList(string subDir, CancellationToken cancellationToken);

		public Task GetFileListAsync(string subDir, CancellationToken cancellationToken);

		public Task<Result> RemoveAsync(string relativePath);

		public IDownloader CreateDownloader();

	}
}
