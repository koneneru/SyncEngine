using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

				OnFetchPlaceholdersAsync(relativePath, opInfo, callbackParameters.FetchPlaceholders.Pattern, kct.Token);
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
			Console.WriteLine("Fetch Data");
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
				HRESULT result = CfExecute(opInfo, ref opParams);

				Console.WriteLine($"Fetch placeholders failed: STATUS_CLOUD_FILE_NETWORK_UNAVAILABLE");

				return;
			}
			
			Console.WriteLine(@"FETCH_DATA: Priority {0}  / R {1} - {2} / O {3} - {4} / {5}",
				callbackInfo.PriorityHint,
				callbackParameters.FetchData.RequiredFileOffset,
				callbackParameters.FetchData.RequiredLength,
				callbackParameters.FetchData.OptionalFileOffset,
				callbackParameters.FetchData.OptionalLength,
				callbackInfo.NormalizedPath);

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

			_ = callbackParameters.FetchData.RequiredFileOffset;

			OnFetchDataAsync(data, callbackInfo, opInfo, new CancellationToken());
		}

		// When the fetch is cancelled, this happens. Our FileCopierWithProgress doesn't really care, because
		// it's fake.
		private void OnCancelFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
			//FileCopierWithProgress.CancelCopyFromServerToClient(callbackInfo, callbackParameters);
		}

		private async void OnFetchPlaceholdersAsync(string subDir, CF_OPERATION_INFO opInfo, string pattern, CancellationToken cancellationToken)
		{
			NtStatus fetchPlaceholdersStatus;
			SafeNativeArray<CF_PLACEHOLDER_CREATE_INFO> createInfo = new();

			// Get file list from server
			//await serverProvider.GetFileListAsync(serverProvider.ConnectionString, cancellationToken);
			var localPlaceholders = (from a in placeholderList where string.Equals(Path.GetDirectoryName(a.RelativePath), subDir) select a).ToList();
			var remoteFilesInfo = (from a in serverProvider.FileList where string.Equals(Path.GetDirectoryName(a.RelativePath), subDir) select a).ToList();

			fetchPlaceholdersStatus = NtStatus.STATUS_SUCCESS;

			if (serverProvider.FileList.Count > 0)
			{
				// Convert Placeholder to CF_PLACEHOLDER_CREATE_INFO
				foreach (var item in remoteFilesInfo)
				{
					if (cancellationToken.IsCancellationRequested) return;
					if (!(from a in localPlaceholders where string.Equals(a.RelativePath, item.RelativePath, StringComparison.CurrentCultureIgnoreCase) select a).Any())
						createInfo.Add(item.ToPlaceholderCreateInfo());
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
				await SynchronizeAsync(subDir, cancellationToken);
			}

			//await GetLocalFileListAsync(subDir, cancellationToken);

			foreach (var item in remoteFilesInfo)
			{
				var localPlaceholder = (from a in localPlaceholders where string.Equals(a.RelativePath, item.RelativePath, StringComparison.CurrentCultureIgnoreCase) select a).FirstOrDefault();

				if (localPlaceholder != null && localPlaceholder.FileAttributes.HasFlag(FileAttributes.Directory) && item.ETag != localPlaceholder.ETag)
					dataProcessor.AddToProcessingQueue(localPlaceholder.RelativePath);
			}

			foreach (var item in localPlaceholders)
			{
				if (!(from a in remoteFilesInfo where string.Equals(a.RelativePath, item.RelativePath, StringComparison.CurrentCultureIgnoreCase) select a).Any())
					dataProcessor.AddToProcessingQueue(item.RelativePath);
			}

			//int i = 0;
			//int j = 0;
			//while (j < remoteFilesInfo.Count)
			//{
			//	//var localPlaceholder = placeholderList[i];
			//	//var remotePlaceholder = serverProvider.FileList[j];
			//	var localPlaceholder = localPlaceholders[i];
			//	var remotePlaceholder = remoteFilesInfo[j];

			//	int comparePathResult = localPlaceholder.RelativePath.CompareTo(remotePlaceholder.RelativePath);
			//	// In that case comparePathResult cannot be greater than zero, because remotePlaceholders list cannot contain
			//	// elements that are not in localPlaceholder list
			//	if (comparePathResult == 0)
			//	{
			//		//dataProcessor.AddToProcessingQueue(localPlaceholder, remotePlaceholder);
			//		dataProcessor.AddToProcessingQueue(localPlaceholder.RelativePath);

			//		#region "Old Implementation"
			//		//if (!localPlaceholder.HasFlag(FileAttributes.Directory))
			//		//{
			//		//	int compareETagResult = localPlaceholder.ETag.CompareTo(remotePlaceholder.ETag);
			//		//	if (compareETagResult < 0)
			//		//	{
			//		//		Change change = new()
			//		//		{
			//		//			relativePath = localPlaceholder.RelativePath,
			//		//			Type = "update",
			//		//			time = DateTime.Now,
			//		//		};
			//		//		dataProcessor.AddToChangeOnServerQueue(change);
			//		//	}
			//		//	if (compareETagResult > 0)
			//		//	{
			//		//		Change change = new()
			//		//		{
			//		//			relativePath = localPlaceholder.RelativePath,
			//		//			Type = "update",
			//		//			time = DateTime.Now,
			//		//		};
			//		//		AddToChangeOnRootQueue(change);
			//		//	}
			//		//}
			//		#endregion

			//		i++;
			//		j++;
			//	}
			//	else
			//	{
			//		//dataProcessor.AddToProcessingQueue(localPlaceholder);
			//		dataProcessor.AddToProcessingQueue(localPlaceholder.RelativePath);

			//		#region "Old Implementation"
			//		//Change change = new()
			//		//{
			//		//	relativePath = localPlaceholder.RelativePath,
			//		//	Type = "add",
			//		//	time = DateTime.Now,
			//		//};
			//		//dataProcessor.AddToChangeOnServerQueue(change);
			//		#endregion

			//		i++;
			//	}
			//}

			//while (i < placeholderList.Count)
			//{
			//	//dataProcessor.AddToProcessingQueue(localPlaceholders[i]);
			//	dataProcessor.AddToProcessingQueue(placeholderList[i].RelativePath);
			//}

			//#region "Old Implementation"
			////if (i == localPlaceholders.Count)
			////{
			////	for (; j < remotePlaceholders.Count; j++)
			////	{
			////		Change change = new()
			////		{
			////			relativePath = remotePlaceholders[j].RelativePath,
			////			Type = "",
			////			time = DateTime.Now,
			////		};
			////		AddToChangeOnRootQueue(change);
			////	}
			////}

			////if (j == remotePlaceholders.Count)
			////{
			////	for (; i < remotePlaceholders.Count; i++)
			////	{
			////		Change change = new()
			////		{
			////			relativePath = remotePlaceholders[i].RelativePath,
			////			Type = "add",
			////			time = DateTime.Now,
			////		};
			////		dataProcessor.AddToChangeOnServerQueue(change);
			////	}
			////}
			//#endregion
		}

		#region "Old Implementation OnFetchPlaceholdersAsync"
		//private async void OnFetchPlaceholdersAsync(string subDir, CF_OPERATION_INFO opInfo, string pattern, CancellationToken cancellationToken)
		//{
		//	NtStatus fetchPlaceholdersStatus;
		//	SafeNativeArray<CF_PLACEHOLDER_CREATE_INFO> createInfo = new();

		//	// Get file list from server
		//	var getRemoteFileListResult = await serverProvider.GetFileList(subDir, cancellationToken);

		//	fetchPlaceholdersStatus = getRemoteFileListResult.Status;
		//	List<FileBasicInfo> remotePlaceholders = getRemoteFileListResult.Data!;

		//	if (getRemoteFileListResult.Succeeded)
		//	{
		//		// Convert Placeholder to CF_PLACEHOLDER_CREATE_INFO
		//		foreach (var item in remotePlaceholders)
		//		{
		//			if (cancellationToken.IsCancellationRequested) return;
		//			createInfo.Add(item.ToPlaceholderCreateInfo());
		//		}
		//	}
		//	else
		//	{
		//		if (getRemoteFileListResult.Status == NtStatus.STATUS_NOT_A_CLOUD_FILE)
		//			fetchPlaceholdersStatus = NtStatus.STATUS_SUCCESS;
		//		else
		//			fetchPlaceholdersStatus = getRemoteFileListResult.Status;
		//	}

		//	uint total = (uint)createInfo.Count;
		//	CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS tpParam = new()
		//	{
		//		Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION,
		//		CompletionStatus = new NTStatus((uint)fetchPlaceholdersStatus),
		//		PlaceholderCount = total,
		//		PlaceholderTotalCount = total,
		//		PlaceholderArray = createInfo,
		//	};
		//	CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(tpParam);
		//	HRESULT executeResult = CfExecute(opInfo, ref opParams);

		//	FetchPlaceholderCancellationTokens.TryRemove(subDir, out _);

		//	if (fetchPlaceholdersStatus != NtStatus.STATUS_SUCCESS || !executeResult.Succeeded)
		//	{
		//              Console.WriteLine($"Failed to fetch placeholders, hr 0x{executeResult:X8}");
		//		return;
		//          }

		//	var localPlaceholders = await GetLocalFileList(subDir, cancellationToken);

		//	int i = 0;
		//	int j = 0;
		//	while(j < remotePlaceholders.Count)
		//	{
		//		var localPlaceholder = localPlaceholders[i];
		//		var remotePlaceholder = remotePlaceholders[j];

		//		int comparePathResult = localPlaceholder.RelativePath.CompareTo(remotePlaceholder.RelativePath);
		//		// In that case comparePathResult cannot be greater than zero, because remotePlaceholders list cannot contain
		//		// elements that are not in localPlaceholder list
		//		if (comparePathResult == 0)
		//		{
		//			//dataProcessor.AddToProcessingQueue(localPlaceholder, remotePlaceholder);
		//			dataProcessor.AddToProcessingQueue(localPlaceholder.RelativePath);

		//			#region "Old Implementation"
		//			//if (!localPlaceholder.HasFlag(FileAttributes.Directory))
		//			//{
		//			//	int compareETagResult = localPlaceholder.ETag.CompareTo(remotePlaceholder.ETag);
		//			//	if (compareETagResult < 0)
		//			//	{
		//			//		Change change = new()
		//			//		{
		//			//			relativePath = localPlaceholder.RelativePath,
		//			//			Type = "update",
		//			//			time = DateTime.Now,
		//			//		};
		//			//		dataProcessor.AddToChangeOnServerQueue(change);
		//			//	}
		//			//	if (compareETagResult > 0)
		//			//	{
		//			//		Change change = new()
		//			//		{
		//			//			relativePath = localPlaceholder.RelativePath,
		//			//			Type = "update",
		//			//			time = DateTime.Now,
		//			//		};
		//			//		AddToChangeOnRootQueue(change);
		//			//	}
		//			//}
		//			#endregion

		//			i++;
		//			j++;
		//		}
		//		else
		//		{
		//			//dataProcessor.AddToProcessingQueue(localPlaceholder);
		//			dataProcessor.AddToProcessingQueue(localPlaceholder.RelativePath);

		//			#region "Old Implementation"
		//			//Change change = new()
		//			//{
		//			//	relativePath = localPlaceholder.RelativePath,
		//			//	Type = "add",
		//			//	time = DateTime.Now,
		//			//};
		//			//dataProcessor.AddToChangeOnServerQueue(change);
		//			#endregion

		//			i++;
		//		}
		//	}

		//	while(i< localPlaceholders.Count)
		//	{
		//		//dataProcessor.AddToProcessingQueue(localPlaceholders[i]);
		//		dataProcessor.AddToProcessingQueue(localPlaceholders[i].RelativePath);
		//	}

		//	#region "Old Implementation"
		//	//if (i == localPlaceholders.Count)
		//	//{
		//	//	for (; j < remotePlaceholders.Count; j++)
		//	//	{
		//	//		Change change = new()
		//	//		{
		//	//			relativePath = remotePlaceholders[j].RelativePath,
		//	//			Type = "",
		//	//			time = DateTime.Now,
		//	//		};
		//	//		AddToChangeOnRootQueue(change);
		//	//	}
		//	//}

		//	//if (j == remotePlaceholders.Count)
		//	//{
		//	//	for (; i < remotePlaceholders.Count; i++)
		//	//	{
		//	//		Change change = new()
		//	//		{
		//	//			relativePath = remotePlaceholders[i].RelativePath,
		//	//			Type = "add",
		//	//			time = DateTime.Now,
		//	//		};
		//	//		dataProcessor.AddToChangeOnServerQueue(change);
		//	//	}
		//	//}
		//	#endregion
		//}
		#endregion

		private async void OnFetchDataAsync(FetchDataParams fetchDataParams, CF_CALLBACK_INFO callbackInfo, CF_OPERATION_INFO opInfo, CancellationToken cancellationToken)
		{			
			Placeholder? placeholder = (from a in placeholderList where string.Equals(fetchDataParams, a.RelativePath) select a).FirstOrDefault();

			if (placeholder == null || placeholder.StandartInfo.InSyncState.HasFlag(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC))
			{
                Console.WriteLine($"Fatching data for {fetchDataParams} failed: STATUS_CLOUD_FILE_NOT_IN_SYNC");

				CF_OPERATION_PARAMETERS.TRANSFERDATA tdParam = new()
				{
					Length = 1,
					Offset = fetchDataParams.FileOffset,
					Buffer = IntPtr.Zero,
					Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
					CompletionStatus = NTStatus.STATUS_UNSUCCESSFUL
				};
				CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(tdParam);
				CfExecute(opInfo, ref opParams);

				return;
			}

			try
			{
				IDownloader downloader = serverProvider.CreateDownloader();
				_ = downloader.StartDownloading(fetchDataParams.RelativePath, cancellationToken);

				byte[] stackBuffer = new byte[stackSize];
				byte[] buffer = new byte[stackSize];
				long startOffset = fetchDataParams.FileOffset;
				long remainingLength = fetchDataParams.Length;

				long total = fetchDataParams.Length;
				long completed = 0;
				
				while(remainingLength > 0)
				{
					int bytesToRead = (int)Math.Min(remainingLength, chunckSize);
					int readBytes = downloader.Read(out buffer, 0, startOffset, bytesToRead);
					NTStatus transferStatus = NTStatus.STATUS_SUCCESS;

					if (readBytes == 0)
					{
						transferStatus = NTStatus.STATUS_UNSUCCESSFUL;

						//CF_OPERATION_PARAMETERS.TRANSFERDATA tdParam = new()
						//{
						//	Length = 1,
						//	Offset = fetchDataParams.FileOffset,
						//	Buffer = IntPtr.Zero,
						//	Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
						//	CompletionStatus = NTStatus.STATUS_UNSUCCESSFUL
						//};
						//CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(tdParam);
						//CfExecute(opInfo, ref opParams);

						//return;
					}

					if (readBytes < bytesToRead && !downloader.IsDownloading)
						transferStatus = NTStatus.STATUS_END_OF_FILE;
										
					IntPtr ptr = IntPtr.Zero;
					Marshal.StructureToPtr(buffer, ptr, true);

					TransferData(
						opInfo,
						readBytes == 0 ? IntPtr.Zero : ptr,
						startOffset,
						readBytes,
						transferStatus);

					completed += readBytes;

					CfReportProviderProgress(opInfo.ConnectionKey, opInfo.TransferKey, total, completed);
				}
			}
			catch (Exception ex)
			{

			}
		}

		private void TransferData(CF_OPERATION_INFO opInfo, IntPtr buffer, long offset, long length, NTStatus completionStatus)
		{
			CF_OPERATION_PARAMETERS.TRANSFERDATA tdParam = new()
			{
				CompletionStatus = NTStatus.STATUS_SUCCESS,
				Buffer = buffer,
				Offset = offset,
				Length = length,	
				Flags = CF_OPERATION_TRANSFER_DATA_FLAGS.CF_OPERATION_TRANSFER_DATA_FLAG_NONE,
			};
			CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(tdParam);
			CfExecute(opInfo, ref opParams);
		}
	}
}
