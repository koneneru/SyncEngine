using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public interface IFilesInfoList : IDisposable
	{
		public Task<Result> FillListAsync(string connectionString, CancellationToken cancellationToken);
		public DataResult<FileBasicInfo> TakeNext();
		public Result Close();
	}
}
