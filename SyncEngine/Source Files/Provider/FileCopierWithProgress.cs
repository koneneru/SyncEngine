using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.System.IO;

namespace SyncEngine
{
	//===============================================================
	// FileCopierWithProgress
	//
	// Fakery Factor:
	//
	//   It's a fabrication to fake a connection to the internet.
	//
	//	 This entire class is completly designed to let the sample go.
	//	 You will want to replace this class with one that actually knows
	//	 how to download stuff from your datacenter and onto the client.
	//	 You can take a look at the code that shows transfer progress,
	//	 that's kinda interesting.
	//
	//	 This code is using the overlapped function for ReadFileEx,
	//	 please see this if you are unfamiliar:
	//
	//		https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-readfileex
	//
	//===============================================================

	// This entire class is static

	public class FileCopierWithProgress
	{
		// Since this is a local disk to local-disk copy, it would happen really fast.
		// This is the size of each chunk to be copied due to the overlapped approach.
		// I pulled this number out of a hat.
		private static readonly int _chunkSize = 4096;
		// Arbitrary delay per chunk, again, so you can actually see the progress bar
		// move.
		private readonly int _chunkDelayms = 250;

		struct READ_COMPLETION_CONTEXT
		{
			internal NativeOverlapped Overlapped;
			internal CF_CALLBACK_INFO CallbackInfo;
			internal string FullPath;
			internal HANDLE Handle;
			internal byte PriorityHint;
			internal long StartOffset;
			internal long RemainingLength;
			internal ulong BufferSize;
			internal byte[] Buffer;
		}

		public static void CopyFromServerToClient(
			in CF_CALLBACK_INFO lpCallbackInfo,
			in CF_CALLBACK_PARAMETERS lpCallbackParameters,
			in string serverFolder)
		{
			try
			{
				CopyFromServerToClientWorker(
					lpCallbackInfo,
					Marshal.PtrToStructure<CF_PROCESS_INFO>(lpCallbackInfo.ProcessInfo),
					lpCallbackParameters.FetchData.RequiredFileOffset,
					lpCallbackParameters.FetchData.RequiredLength,
					lpCallbackParameters.FetchData.OptionalFileOffset,
					lpCallbackParameters.FetchData.OptionalLength,
					lpCallbackParameters.FetchData.Flags,
					lpCallbackInfo.PriorityHint,
					serverFolder);
			}
			catch
			{
				TransferData(
					lpCallbackInfo.ConnectionKey,
					lpCallbackInfo.TransferKey,
					IntPtr.Zero,
					lpCallbackParameters.FetchData.RequiredFileOffset,
					lpCallbackParameters.FetchData.RequiredLength,
					NTStatus.STATUS_UNSUCCESSFUL);
			}
		}

		public static void CancelCopyFromServerToClient(
			in CF_CALLBACK_INFO lpCallbackInfo,
			in CF_CALLBACK_PARAMETERS lpCallbackParameters)
		{
			CancelCopyFromServerToClientWorker(
				lpCallbackInfo,
				lpCallbackParameters.Cancel.FetchData.FileOffset,
				lpCallbackParameters.Cancel.FetchData.Length,
				lpCallbackParameters.Cancel.Flags);
		}

		private static void CopyFromServerToClientWiorker(in CF_CALLBACK_INFO callbackInfo,
			CF_PROCESS_INFO processInfo,
			in long requiredFileOffset,
			in long requiredLength,
			in long optionalFileOffset,
			in long optionalLength,
			in CF_CALLBACK_FETCH_DATA_FLAGS fetchFlags,
			in byte priorityHint,
			in string serverFolder)
		{
			string fullServerPath = new StringBuilder(serverFolder)
				.Append(callbackInfo.NormalizedPath[callbackInfo.NormalizedPath.LastIndexOf('\\')..]).ToString();


			string fullClientPath = new StringBuilder(callbackInfo.VolumeDosName)
				.Append(callbackInfo.NormalizedPath).ToString();

			long chunckBufferSize;

			File.OpenRead(fullServerPath);
		}

		#region "Old Implementation"
		// In a nutshell, it copies a file from the "server" to the
		// "client" using the overlapped trickery of Windows to
		// chunkanize the copy. This way you don't have to allocate
		// a huge buffer.
		private static void CopyFromServerToClientWorker(
			in CF_CALLBACK_INFO callbackInfo,
			CF_PROCESS_INFO processInfo,
			in long requiredFileOffset,
			in long requiredLength,
			in long optionalFileOffset,
			in long optionalLength,
			in CF_CALLBACK_FETCH_DATA_FLAGS fetchFlags,
			in byte priorityHint,
			in string serverFolder)
		{
			SafeFileHandle serverFileHandle;

			string fullServerPath = new StringBuilder(serverFolder)
				.Append(callbackInfo.NormalizedPath[callbackInfo.NormalizedPath.LastIndexOf('\\')..]).ToString();


			string fullClientPath = new StringBuilder(callbackInfo.VolumeDosName)
				.Append(callbackInfo.NormalizedPath).ToString();

			READ_COMPLETION_CONTEXT readCompletionContext;
			ulong chunkBufferSize;

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

			serverFileHandle = PInvoke.CreateFileW(
				fullServerPath,
				FILE_ACCESS_FLAGS.FILE_GENERIC_READ,
				FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_DELETE,
				null,
				FILE_CREATION_DISPOSITION.OPEN_EXISTING,
				FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED,
				null);

			var serverFileHandle2 = File.OpenHandle(
				fullServerPath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read | FileShare.Delete,
				FileOptions.Asynchronous);

			if (serverFileHandle.IsInvalid)
			{
				HRESULT hr = Marshal.GetLastWin32Error();
                Console.WriteLine("[0x{0:X4}:0x{1:X4}] - Failed to open {2} for read, hr 0x{3:X8}",
					Process.GetCurrentProcess().Id,
					Process.GetCurrentProcess().Threads[0].Id,
					fullServerPath,
					hr);

				hr.ThrowIfFailed();
            }

			chunkBufferSize = (ulong)Math.Min(requiredLength, _chunkSize);

			readCompletionContext = new READ_COMPLETION_CONTEXT();
			{
				readCompletionContext.FullPath = fullClientPath;
				readCompletionContext.CallbackInfo = callbackInfo;
				readCompletionContext.Handle = serverFileHandle;
				readCompletionContext.PriorityHint = priorityHint;
				readCompletionContext.Overlapped = new()
				{
					OffsetLow = requiredFileOffset.HighPart(),
					OffsetHigh = requiredFileOffset.HighPart()
				};
				readCompletionContext.StartOffset = requiredFileOffset;
				readCompletionContext.RemainingLength = requiredLength;
				readCompletionContext.BufferSize = chunkBufferSize;
				readCompletionContext.Buffer = new byte[chunkBufferSize];
			}

            Console.WriteLine("[0x{0}:0x{1}] - Downloading data for {2}, priority {3}, offset 0x{4:X8}`0x{5:X8} length 0x{6:X8}",
				Process.GetCurrentProcess().Id,
				Process.GetCurrentProcess().Threads[0].Id,
				readCompletionContext.FullPath,
				priorityHint,
				requiredFileOffset.HighPart(),
				requiredFileOffset.LowPart(),
				chunkBufferSize);

			// Initiate the read for the first chunk. When This async operation
			// completes (failure or success), it will call the OverlappedCompletionRoutine
			// above with that chunk. That OverlappedCompletionRoutine is responsible for
			// subsequent ReadFileEx calls to read subsequent chunks. This is only for the
			// first one
			unsafe
			{
				if (!PInvoke.ReadFileEx(
				serverFileHandle,
				&readCompletionContext.Buffer,
				(uint)chunkBufferSize,
				ref readCompletionContext.Overlapped,
				OverlappedComletionRoute))
				{
					HRESULT hr = Marshal.GetLastWin32Error();
                    Console.WriteLine("[0x{0:X4}:0x{1:X4}] - Failed to perform async read for {2}, Status {3}",
						Process.GetCurrentProcess().Id,
						Process.GetCurrentProcess().Threads[0].Id,
						fullServerPath,
						hr);

					serverFileHandle.Close();

					hr.ThrowIfFailed();
				}
			}

			FileStream fs = new(serverFileHandle2, FileAccess.Read, (int)chunkBufferSize, true);
		}
		#endregion

		private static void CancelCopyFromServerToClientWorker(
			in CF_CALLBACK_INFO callbackInfo,
			in long cancelFileOffset,
			in long cancelLength,
			in CF_CALLBACK_CANCEL_FLAGS cancelFlags)
		{
            // Yeah, a whole lotta nothing happens here, because sample.
            Console.WriteLine("[0x{0:X4}:0x{1:X4}] - Cancelling read for {2}{3}, offset 0x{4:X8}`0x{5:X8} length 0x{6:X8}`0x{7:X8}",
				Process.GetCurrentProcess().Id,
				Process.GetCurrentProcess().Threads[0].Id,
				callbackInfo.VolumeDosName,
				callbackInfo.NormalizedPath,
				cancelFileOffset.HighPart(),
				cancelFileOffset.LowPart(),
				cancelLength.HighPart(),
				cancelLength.LowPart());
        }

		private unsafe static void OverlappedComletionRoute(
			uint errorCode,
			uint numberOfBytesTransfered,
			NativeOverlapped* overlapped)
		{
			var keepProcessing = false;

			//do
			//{
			//	// Determines how many bytes have been "downloaded"
			//	if(errorCode == 0)
			//	{
			//		if(!PInvoke.GetOverlappedResult(serverFile))
			//	}
			//}
		}

		private static void TransferData(
			in CF_CONNECTION_KEY connectionKey,
			in CF_TRANSFER_KEY transferKey,
			in IntPtr transferData,
			in long strartingOffset,
			in long length,
			in NTStatus completionStatus)
		{
			var opInfo = new CF_OPERATION_INFO()
			{
				Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA,
				ConnectionKey = connectionKey,
				TransferKey = transferKey
			};
			opInfo.StructSize = (uint)Marshal.SizeOf(opInfo);

			var opParams = new CF_OPERATION_PARAMETERS()
			{
				TransferData = new CF_OPERATION_PARAMETERS.TRANSFERDATA
				{
					CompletionStatus = completionStatus,
					Buffer = transferData,
					Offset = strartingOffset,
					Length = length
				}
			};
			opParams.ParamSize= (uint)Marshal.SizeOf(opParams.TransferData);

			// Perhaps a custom logger wil be used
			CfExecute(opInfo, ref opParams).ThrowIfFailed();
		}
	}
}
