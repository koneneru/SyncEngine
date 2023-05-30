using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Windows.ApplicationModel;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Kernel32;

namespace SyncEngine
{
	public partial class SyncRoot
	{
		private CF_CALLBACK_REGISTRATION[] s_SECallbackTable;
		private readonly ConcurrentDictionary<string, CancellationTokenSource> FetchPlaceholderCancellationTokens = new();

		private void OnFetchPlaceholders(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
            Console.WriteLine("Fetch Placeholders");
			var opInfo = CloudApiHelper.CreateOperationInfo(callbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS);

			if (serverProvider.IsConnected)
			{
				string relativePath = Path.GetRelativePath(localRootFolder, callbackInfo.NormalizedPath);
				relativePath = relativePath == "." ? string.Empty : relativePath;

				CancellationTokenSource kct = new ();

				FetchPlaceholderCancellationTokens.AddOrUpdate(relativePath, kct, (k, v) =>
				{
					v?.Cancel();
					return kct;
				});

				OnFetchPlaceholdersAsync(relativePath, opInfo, kct.Token);
			}
			else
			{
				CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS tpParam = new()
				{
					PlaceholderArray = IntPtr.Zero,
					Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_NONE,
					PlaceholderCount = 0,
					PlaceholderTotalCount = 0,
					CompletionStatus = new NTStatus((uint)NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE),
				};
				CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(tpParam);
				HRESULT result = CfExecute(opInfo, ref opParams);

				Console.WriteLine($"Fetch placeholders failed: STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE");

                return;
			}				
		}

		private void OnCancelFetchPlaceholders(in CF_CALLBACK_INFO callbackinfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
			string relativePath = Path.GetRelativePath(localRootFolder, callbackinfo.NormalizedPath);
			relativePath = relativePath == "." ? string.Empty : relativePath;

			Console.WriteLine($"Cancel Fetch Placeholders for \\{relativePath}");

			if (FetchPlaceholderCancellationTokens.TryRemove(relativePath, out CancellationTokenSource? kct))
			{
				kct?.Cancel();
                Console.WriteLine($"Fetch Placeholders for \\{relativePath} cancelled");
            }
		}

		private void OnFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
			Console.WriteLine("[0x{0:X4}:0x{1:X4}] - Recieved data request for {2}{3}, priority {4}, offset 0x{5:X8}`0x{6:X8} length 0x{7:X8}`0x{8:X8}",
				Process.GetCurrentProcess().Id,
				Process.GetCurrentProcess().Threads[0].Id,
				callbackInfo.VolumeDosName,
				callbackInfo.NormalizedPath,
				callbackInfo.PriorityHint,
				callbackParameters.FetchData.RequiredFileOffset.HighPart(),
				callbackParameters.FetchData.RequiredFileOffset.LowPart(),
				callbackParameters.FetchData.RequiredLength.HighPart(),
				callbackParameters.FetchData.RequiredLength.LowPart());

			var opInfo = CloudApiHelper.CreateOperationInfo(callbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA);

			if (!serverProvider.IsConnected)
			{
				CF_OPERATION_PARAMETERS.TRANSFERDATA tdParam = new()
				{
					Length = callbackParameters.FetchData.RequiredLength,
					Offset = callbackParameters.FetchData.RequiredFileOffset,
					Buffer = IntPtr.Zero,
					Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
					CompletionStatus = new NTStatus((uint)NtStatus.STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE)
				};
				CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(tdParam);
				CfExecute(opInfo, ref opParams);

				Console.WriteLine($"Fetch data failed: STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE");

				return;
			}

			string relativePath = Path.GetRelativePath(localRootFolder, callbackInfo.NormalizedPath);
			relativePath = relativePath == "." ? string.Empty : relativePath;

			FetchDataParams data = new()
			{
				FileOffset = callbackParameters.FetchData.RequiredFileOffset,
				Length = callbackParameters.FetchData.RequiredLength,
				RelativePath = relativePath,
				PriorityHint = callbackInfo.PriorityHint,
				TransferKey = callbackInfo.TransferKey,
			};

			OnFetchDataAsync(data, callbackInfo, opInfo, new CancellationToken());
		}

		// When the fetch is cancelled, this happens. Our FileCopierWithProgress doesn't really care, because
		// it's fake.
		private void OnCancelFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
			//FileCopierWithProgress.CancelCopyFromServerToClient(callbackInfo, callbackParameters);
		}

		private async void OnFetchPlaceholdersAsync(string subDir, CF_OPERATION_INFO opInfo, CancellationToken cancellationToken)
		{
			NtStatus fetchPlaceholdersStatus;
			SafeNativeArray<CF_PLACEHOLDER_CREATE_INFO> createInfo = new();

			var localPlaceholders = (from a in placeholderList.Keys where string.Equals(Path.GetDirectoryName(a), subDir) select a).ToList();
			var remoteFilesInfo = (from a in serverProvider.FileList.Keys where string.Equals(Path.GetDirectoryName(a), subDir) select a).ToList();

			fetchPlaceholdersStatus = NtStatus.STATUS_SUCCESS;

			if (remoteFilesInfo.Count > 0)
			{
				// Convert Placeholder to CF_PLACEHOLDER_CREATE_INFO
				foreach (var item in remoteFilesInfo)
				{
					if (cancellationToken.IsCancellationRequested) return;
					if (!placeholderList.ContainsKey(item))
						createInfo.Add(serverProvider.FileList[item].ToPlaceholderCreateInfo());
				}
			}
			else
			{
				fetchPlaceholdersStatus = NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL;
			}

			uint total = (uint)createInfo.Count;
			CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS tpParam = new()
			{
				Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION,
				CompletionStatus = new NTStatus((uint)fetchPlaceholdersStatus),
				PlaceholderCount = total,
				PlaceholderTotalCount = total,
				PlaceholderArray = createInfo,
			};
			CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(tpParam);
			HRESULT executeResult = CfExecute(opInfo, ref opParams);

			FetchPlaceholderCancellationTokens.TryRemove(subDir, out _);

			if (fetchPlaceholdersStatus != NtStatus.STATUS_SUCCESS || !executeResult.Succeeded)
			{
				Console.WriteLine($"Failed to fetch placeholders, hr 0x{executeResult:X8}");
				return;
			}
			else
			{
				await LoadFileListAsync(subDir, cancellationToken);
			}

			foreach (var item in remoteFilesInfo)
			{
				var localPlaceholder = placeholderList[item];

				if (!localPlaceholder.FileAttributes.HasFlag(FileAttributes.Directory) && serverProvider.FileList[item].ETag != localPlaceholder.ETag)
					dataProcessor.AddToProcessingQueue(localPlaceholder.RelativePath);
			}

			foreach (var item in localPlaceholders)
			{
				if (!serverProvider.FileList.ContainsKey(item))
					dataProcessor.AddToProcessingQueue(item);
			}
		}

		private async void OnFetchDataAsync(FetchDataParams fetchDataParams, CF_CALLBACK_INFO callbackInfo, CF_OPERATION_INFO opInfo, CancellationToken cancellationToken)
		{
			if (!placeholderList.ContainsKey(fetchDataParams.RelativePath))// ||
				//placeholderList[fetchDataParams.RelativePath].StandartInfo.InSyncState.HasFlag(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC))
			{
				Console.WriteLine($"Fetching data for {fetchDataParams} failed: STATUS_CLOUD_FILE_NOT_IN_SYNC");

				TransferData(
						opInfo,
						IntPtr.Zero,
						fetchDataParams.FileOffset,
						1,
						new NTStatus((uint)NtStatus.STATUS_NOT_A_CLOUD_FILE));

				return;
			}

			IntPtr ptr = IntPtr.Zero;
			try
			{
				IDownloader downloader = serverProvider.CreateDownloader();
				_ = downloader.StartDownloading(fetchDataParams.RelativePath, cancellationToken);

				byte[] buffer = new byte[stackSize];
				long startOffset = fetchDataParams.FileOffset;
				long remainingLength = fetchDataParams.Length;

				long total = fetchDataParams.Length;
				long completed = 0;

				unsafe
				{
					fixed (void* bufferPtr = buffer)
					{

						while (remainingLength > 0)
						{
							int bytesToRead = (int)Math.Min(remainingLength, stackSize);
							int readBytes = downloader.Read(out byte[] readBuffer, 0, startOffset, bytesToRead);
							NTStatus transferStatus = NTStatus.STATUS_SUCCESS;

							if (downloader.Status == DownloadingStatus.Failed)
								transferStatus = NTStatus.STATUS_UNSUCCESSFUL;

							if (remainingLength == 0 && downloader.Status == DownloadingStatus.Completed)
								transferStatus = NTStatus.STATUS_END_OF_FILE;

							Marshal.Copy(readBuffer, 0, (IntPtr)bufferPtr, readBytes);

							TransferData(
								opInfo,
								readBytes == 0 ? IntPtr.Zero : (IntPtr)bufferPtr,
								startOffset,
								Math.Min(readBytes, remainingLength),
								transferStatus);

							startOffset += readBytes;
							completed += readBytes;
							remainingLength -= readBytes;

							CfReportProviderProgress(opInfo.ConnectionKey, opInfo.TransferKey, total, completed);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"OnFetchDataAsync for {fetchDataParams} failed with {ex.Message}");
			}
		}

		private static void TransferData(CF_OPERATION_INFO opInfo, IntPtr buffer, long offset, long length, NTStatus completionStatus)
		{
			CF_OPERATION_PARAMETERS.TRANSFERDATA tdParam = new()
			{
				Buffer = buffer,
				Offset = offset,
				Length = length,	
				Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
				CompletionStatus = completionStatus
			};
			CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(tdParam);
			CfExecute(opInfo, ref opParams);
		}
	}
}
