using SyncEngine.ServerProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement.Core;

namespace SyncEngine
{
	internal class LocalServerFilesInfoList : IFilesInfoList
	{
		private readonly LocalServerProvider _provider;
		private readonly CancellationTokenSource _konCT = new();
		private readonly Queue<KeyValuePair<string, WIN32_FIND_DATA>> _filesInfo = new();
		private bool reading = false;
		private Result status;

        public LocalServerFilesInfoList(in LocalServerProvider provider)
        {
            _provider = provider;
        }

        public Task<Result> FillListAsync(string subDir, CancellationToken cancellationToken)
		{
			if(!_provider.IsConnected)
				return Task.FromResult(new Result(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE));

			cancellationToken.Register(() => _konCT.Cancel());

			string fullPath = Path.Combine(_provider.ConnectionString, subDir);
			string filePattern = Path.Combine(fullPath, "*");

			if(!Directory.Exists(fullPath))
				return Task.FromResult(new Result(NtStatus.STATUS_NOT_A_CLOUD_FILE));

			reading = true;
			Task.Run(()=>ReadFilesInfoRecurcive(subDir), _konCT.Token);

			return Task.FromResult(new Result());
		}

		public DataResult<FileBasicInfo> TakeNext()
		{
			DataResult<FileBasicInfo> result;

			if (_filesInfo.Count != 0)
			{
				var fileInfo = _filesInfo.Dequeue();
				FileBasicInfo placeholder = new(fileInfo.Key, fileInfo.Value);
				result = new DataResult<FileBasicInfo>(placeholder);
			}
			else
			{
				if (reading)
				{
					// Returns STATUS_SUCCESS with Data = null, because not all files info
					// was recieved from "server"
					result = new DataResult<FileBasicInfo>();
				}
				else result = new DataResult<FileBasicInfo>(NtStatus.STATUS_UNSUCCESSFUL);
			}

			return result;
		}

		public Result Close()
		{
			_konCT?.Cancel();
			if (reading) status = new(NtStatus.STATUS_CLOUD_FILE_REQUEST_ABORTED);

			return status;
		}

		private void ReadFilesInfoRecurcive(string subDir)
		{
			try
			{
				string fullPath = Path.Combine(_provider.ConnectionString, subDir);
				string filePattern = Path.Combine(fullPath, "*");

				var hFileHandle = Kernel32.FindFirstFileEx(
					filePattern,
					Kernel32.FINDEX_INFO_LEVELS.FindExInfoStandard,
					out WIN32_FIND_DATA findData,
					Kernel32.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
					IntPtr.Zero,
					Kernel32.FIND_FIRST.FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY);

				if (!hFileHandle.IsInvalid)
				{
					do
					{
						_konCT.Token.ThrowIfCancellationRequested();

						if (findData.cFileName == "." || findData.cFileName == "..")
						{
							continue;
						}

						string relativePath = Path.Combine(subDir, findData.cFileName);

						_filesInfo.Enqueue(new KeyValuePair<string, WIN32_FIND_DATA>(relativePath, findData));

						//if (findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
						//{
						//	ReadFilesInfoRecurcive(relativePath);
						//}
					}
					while (Kernel32.FindNextFile(hFileHandle, out findData));

					hFileHandle.Close();
				}
			}
			catch (Exception ex)
			{
				status = new(ex);
			}
			finally
			{
				reading = false;
			}
		}

		#region "Dispose"
		public void Dispose()
		{
			_konCT?.Cancel();
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
