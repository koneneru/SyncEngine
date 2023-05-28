using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public interface IDownloader
	{
		public DownloadingStatus Status { get; }

		public Task StartDownloading(string path, CancellationToken cancellationToken);

		public int Read(out byte[] buffer, int buferOffset, long offset, int count);
	}
}
