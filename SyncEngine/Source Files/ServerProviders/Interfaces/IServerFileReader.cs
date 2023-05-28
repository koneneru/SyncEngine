using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine.ServerProviders
{
	public interface IServerFileReader
	{
		/// <summary>
		/// This is called at the begginning of a file transfer, just after instance creation and before the first call of ReadAsync()
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		Task<OpenFileResult> OpenFileAsync(in CF_CALLBACK_INFO callbackInfo,
			CF_PROCESS_INFO processInfo,
			in long requiredFileOffset,
			in long requiredLength,
			in long optionalFileOffset,
			in long optionalLength,
			in CF_CALLBACK_FETCH_DATA_FLAGS fetchFlags,
			in byte priorityHint,
			in string serverFolder);

		/// <summary>
		/// Reads up to <paramref name="count"/> bytes of a file, statrting at the position <paramref name="offset"/>,
		/// and writes tho the <paramref name="buffer"/>.
		/// </summary>
		/// <param name="buffer">The buffer where readed bytes should be written.</param>
		/// <param name="bufferOffset">Offset of the <paramref name="buffer"/> where to start write to.</param>
		/// <param name="offset">Offset of a file to start reading.</param>
		/// <param name="count">The max number of bytes that will be read.</param>
		/// <returns></returns>
		Task<ReadFileResult> ReadFileAsync(byte[] buffer, int bufferOffset, long offset, int count);

		Task<CloseFileResult> CloseAsync();
	}
}
