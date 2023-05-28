using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine.ServerProviders
{
	internal class ServerFileReader : IServerFileReader
	{
		private readonly IServerProvider serverProvider;
		private FileStream? fileStream;

        public ServerFileReader(in IServerProvider provider)
        {
            serverProvider = provider;
        }

		//private struct READ_COMPLETION_CONTEXT
		//{
		//	CF_CALLBACK_INFO CallbackInfo;
		//	string FullPath;
		//	HANDLE Handle;
		//	byte PriorityHint;
		//	long startOffset;
		//	long RemainingLength;
		//	long BufferSize;
		//	byte[] Buffer;
		//}

		public Task<CloseFileResult> CloseAsync()
		{
			fileStream?.Close();
			
			return Task.FromResult(new CloseFileResult());
		}

		public Task<OpenFileResult> OpenFileAsync(in CF_CALLBACK_INFO callbackInfo,
			CF_PROCESS_INFO processInfo,
			in long requiredFileOffset,
			in long requiredLength,
			in long optionalFileOffset,
			in long optionalLength,
			in CF_CALLBACK_FETCH_DATA_FLAGS fetchFlags,
			in byte priorityHint,
			in string serverFolder)
		{
			if(!serverProvider.IsConnected)
			{
				return Task.FromResult(new OpenFileResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE));
			}

			OpenFileResult openResult;

			string fullServerPath = new StringBuilder(serverFolder)
				.Append(callbackInfo.NormalizedPath[callbackInfo.NormalizedPath.LastIndexOf('\\')..]).ToString();

			Console.WriteLine("[0x{0:X4}:0x{1:X4}] - Recieved data request from {2} for {3}{4}, priority {5}, offset 0x{6:X8}`0x{7:X8} length 0x{8:X8}`0x{9:X8}",
				Process.GetCurrentProcess().Id,
				Process.GetCurrentProcess().Threads[0].Id,
				processInfo.ImagePath,
				callbackInfo.VolumeDosName,
				callbackInfo.NormalizedPath,
				priorityHint,
				requiredFileOffset.HighPart(),
				requiredFileOffset.LowPart(),
				requiredLength.HighPart(),
				requiredLength.LowPart());

			try
			{
				fileStream = File.OpenRead(fullServerPath);
				openResult = new();
			}
			catch(Exception ex)
			{
				Console.WriteLine("[0x{0:X4}:0x{1:X4}] - Failed to open {2} for read, hr 0x{3:X8}",
					Process.GetCurrentProcess().Id,
					Process.GetCurrentProcess().Threads[0].Id,
					fullServerPath,
					ex.HResult);
				openResult = new(ex);
			}

			return Task.FromResult(openResult);
		}

		public async Task<ReadFileResult> ReadFileAsync(byte[] buffer, int bufferOffset, long offset, int count)
		{
			if (!serverProvider.IsConnected)
			{
				return new ReadFileResult(NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE);
			}

			if (fileStream == null) return new ReadFileResult(NtStatus.ERROR_IO_PENDING);

			ReadFileResult readResult;

			try
			{
				fileStream.Position = offset;
				readResult = new ReadFileResult(await fileStream.ReadAsync(buffer, bufferOffset, count));
			}
			catch (Exception ex)
			{
                Console.WriteLine("[0x{0:X4}:0x{1:X4}] - Async read failed for {2}, with hr 0x{3:X8}",
					Process.GetCurrentProcess().Id,
					Process.GetCurrentProcess().Threads[0].Id,
					fileStream.Name,
					ex.HResult);

                readResult = new(ex);
			}

			return readResult;
		}
	}
}
