using SyncEngine.ServerProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public enum DownloadingStatus : short
	{
		WaitingStart = 0,
		Downloading = 1,
		Completed = 2,

		Failed = -1
	}

	public class LocalDownloader : IDownloader
	{
		private readonly LocalServerProvider _provider;
		private MemoryStream? _stream;
		private DownloadingStatus _status = DownloadingStatus.WaitingStart;
		private string _path = string.Empty;

		public LocalDownloader(LocalServerProvider provider)
		{
			_provider = provider;
		}

		public DownloadingStatus Status { get { return _status; } }

		public int Read(out byte[] buffer, int bufferOffset, long offset, int count)
		{
			long readBytes = 0;
			buffer = Array.Empty<byte>();
			try
			{
				byte[] streamBuffer;// = _stream?.GetBuffer();
				try
				{
					readBytes = _stream!.Position;
					streamBuffer = _stream.GetBuffer();
				}
				catch
				{
					streamBuffer = _stream!.GetBuffer();
					readBytes = streamBuffer.Length;
				}

				readBytes -= offset;
				readBytes = Math.Min(readBytes, count);
				if (readBytes > 0)
				{
					buffer = new byte[readBytes];
					Array.Copy(streamBuffer, offset, buffer, bufferOffset, readBytes);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed read {_path} bytes[{offset} - {offset + readBytes}]: {ex.Message}");
            }
			finally
			{
				if ((_status == DownloadingStatus.Completed || _status == DownloadingStatus.Completed) && readBytes == 0)
				{
					_stream?.Dispose();
				}
			}

			return readBytes < 0 ? 0 : (int)readBytes;
		}

		public async Task StartDownloading(string path, CancellationToken cancellationToken)
		{
			if (!_provider.IsConnected) return;
			
			_path = path;
			_stream = new MemoryStream();
			string fullPath = Path.Combine(_provider.ConnectionString, _path);
			_status = DownloadingStatus.Downloading;
			try
			{
				await _provider.DownloadFileAsync(fullPath, _stream, cancellationToken);
				_status = DownloadingStatus.Completed;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Downloading {_path} failed: {ex.Message}");
				_status = DownloadingStatus.Failed;
				_stream.Dispose();
			}
		}
	}
}
