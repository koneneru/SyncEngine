using SyncEngine.ServerProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class LocalDownloader : IDownloader
	{
		private readonly LocalServerProvider _provider;
		private MemoryStream? _stream;
		private bool _downloading;
		private string _path = string.Empty;

		public LocalDownloader(LocalServerProvider provider)
		{
			_provider = provider;
		}

		public bool IsDownloading { get { return _downloading; } }

		public int Read(out byte[] buffer, int bufferOffset, long offset, int count)
		{
			int bytesRead = 0;
			buffer = new byte[count];
			try
			{
				_stream!.Position = offset;
				bytesRead = _stream.Read(buffer, bufferOffset, count);
			}
			catch (Exception ex)
			{
                Console.WriteLine($"Failed read {_path} bytes[{offset} - {offset+count}]: {ex.Message}");
            }
			finally
			{
				if(!_downloading && bytesRead == 0)
				{
					_stream?.Dispose();
				}
			}

			return bytesRead;
		}

		public async Task StartDownloading(string path, CancellationToken cancellationToken)
		{
			if (!_provider.IsConnected) return;
			
			_path = path;
			_stream = new MemoryStream();
			string fullPath = Path.Combine(_provider.ConnectionString, _path);
			_downloading = true;
			try
			{
				await _provider.DownloadFileAsync(fullPath, _stream, cancellationToken);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Downloading {_path} failed: {ex.Message}");
				_stream.Dispose();
			}
			finally { _downloading = false; }
		}
	}
}
