using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine
{
	public class DataProcessor
	{
		SyncContext syncContext;

		CancellationTokenSource DataProcessingCancellationTokenSource = new();

		public readonly ChangesQueue<Change> LocalChanges = new();
		public readonly ChangesQueue<Change> RemoteChanges = new();

		private readonly Task ProcessingDataLocalTask;
		private readonly Task ProcessingDataRemoteTask;

		private ActionBlock<Change> RemoteChangesProcessing;
		private ActionBlock<Change> LocalChangesProcessing;

		private HashSet<Change> inProcessing = new();

		public DataProcessor(SyncContext context)
		{
			this.syncContext = context;

			ProcessingDataLocalTask = RunRemoteChangesProcessingTask();
			ProcessingDataRemoteTask = RunLocalChangesProcessingTask();

			RemoteChangesProcessing = new(ProcessRemoteChange, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = 8,
				CancellationToken = DataProcessingCancellationTokenSource.Token
			});
			LocalChangesProcessing = new(ProcessLocalChange, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = 8,
				CancellationToken = DataProcessingCancellationTokenSource.Token
			});
		}

		public void AddLocalChange(Change change)
		{
			if (!syncContext.SyncRoot.placeholderList.ContainsKey(change.RelativePath))
				change.Type = ChangeType.Created;
			else
			{
				if (change.Type == ChangeType.Modified)
				{
					string fullPath = Path.Combine(syncContext.SyncRoot.Root, change.RelativePath);
					var fileHandle = Kernel32.FindFirstFile(fullPath, out WIN32_FIND_DATA findData);
					try
					{
						if (!fileHandle.IsInvalid)
						{
							var newPlaceholder = new Placeholder(syncContext.SyncRoot.Root, change.RelativePath, findData);

							fileHandle.Close();

							var currentPlaceholder = syncContext.SyncRoot.placeholderList[change.RelativePath];

							if (newPlaceholder.StandartInfo.ModifiedDataSize == 0)
							{
								if (currentPlaceholder.FileAttributes != newPlaceholder.FileAttributes)
								{
									currentPlaceholder = newPlaceholder;
									change.Type = ChangeType.State;
									syncContext.SyncRoot.placeholderList[change.RelativePath] = newPlaceholder;
								}
								else return;
							}
						}
					}
					finally { fileHandle?.Close(); }
				}
			}
			
			LocalChanges.Enqueue(change);
			Console.WriteLine($"[DataProcessor: 73] Added {change.RelativePath} to LocalChanges as {change.Type}");
		}

		//public void AddLocalChange(Change change)
		//{
		//	if (change.Type == ChangeType.Modified)
		//	{
		//		string fullPath = Path.Combine(syncContext.SyncRoot.Root, change.RelativePath);
		//		var fileHandle = Kernel32.FindFirstFile(fullPath, out WIN32_FIND_DATA findData);
		//		try
		//		{
		//			if (!fileHandle.IsInvalid)
		//			{
		//				var newPlaceholder = new Placeholder(syncContext.SyncRoot.Root, change.RelativePath, findData);

		//				fileHandle.Close();

		//				var currentPlaceholder = syncContext.SyncRoot.placeholderList[change.RelativePath];

		//				if (newPlaceholder.StandartInfo.ModifiedDataSize == 0)
		//				{
		//					if (currentPlaceholder.FileAttributes != newPlaceholder.FileAttributes)
		//					{
		//						currentPlaceholder = newPlaceholder;
		//						change.Type = ChangeType.State;
		//						syncContext.SyncRoot.placeholderList[change.RelativePath] = newPlaceholder;
		//					}
		//					else return;
		//				}
		//			}
		//		}
		//		finally { fileHandle?.Close(); }
		//	}

		//	LocalChanges.Enqueue(change);
		//	Console.WriteLine($"[DataProcessor: 73] Added {change.RelativePath} to LocalChanges as {change.Type}");
		//}

		private Task RunRemoteChangesProcessingTask()
		{
			return Task.Factory.StartNew(async () =>
			{
				while (!DataProcessingCancellationTokenSource.IsCancellationRequested)
				{
					if (RemoteChanges.Dequeue(out var item, DataProcessingCancellationTokenSource.Token))
					{
						try
						{
							if (item.RelativePath == "." || item.RelativePath == "..")
								continue;

							//await RemoteChangesProcessing.SendAsync(item);
							await ProcessRemoteChange(item);
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Failed to process changes of {item.RelativePath}: {ex.Message}");
						}
					}
				}
			}, DataProcessingCancellationTokenSource.Token, TaskCreationOptions.LongRunning |
			TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap();
		}

		private Task RunLocalChangesProcessingTask()
		{
			return Task.Factory.StartNew(async () =>
			{
				while (!DataProcessingCancellationTokenSource.IsCancellationRequested)
				{
					if (LocalChanges.Dequeue(out var item, DataProcessingCancellationTokenSource.Token))
					{
						try
						{
							if (item.RelativePath == "." || item.RelativePath == "..")
								continue;

							if(inProcessing.Add(item))
							{
								//await LocalChangesProcessing.SendAsync(item);
								await ProcessLocalChange(item);
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Failed to process changes of {item.RelativePath}: {ex.Message}");
						}
					}
				}
			}, DataProcessingCancellationTokenSource.Token, TaskCreationOptions.LongRunning |
			TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap();
		}

		#region "Local processing"
		private async Task ProcessLocalChange(Change change)
		{
			Task task = change.Type switch
			{
				ChangeType.Created => AddToServerAsync(change),
				ChangeType.Deleted => DeleteFromServerAsync(change),
				ChangeType.Modified => ModifyOnServerAsync(change),
				ChangeType.State => ChangeStateAsync(change),
				_ => throw new NotSupportedException()
			};

			await task;
			inProcessing.Remove(change);
		}

		private async Task<Result> AddToServerAsync(Change change)
		{
			Result uploadResult;

			string fullPath = syncContext.SyncRoot.GetFullPath(change.RelativePath);
			syncContext.SyncRoot.AddPlaceholderToList(change.RelativePath);

			if (Directory.Exists(fullPath))
			{
				uploadResult = await syncContext.ServerProvider.CreateDirectoryAsync(change.RelativePath);
			}
			else
			{
				uploadResult = await syncContext.SyncRoot.UploadFileAsync(change.RelativePath, UploadMode.Create, DataProcessingCancellationTokenSource.Token);
			}

			if (uploadResult.Succeeded)
			{
				var placeholder = syncContext.SyncRoot.placeholderList[change.RelativePath];
				placeholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
			}

			return uploadResult;
		}

		private async Task<Result> DeleteFromServerAsync(Change change)
		{
			return await syncContext.SyncRoot.DeleteRemoteAsync(change.RelativePath);
		}

		private async Task<Result> ModifyOnServerAsync(Change change)
		{
			Result modifyResult;

			Placeholder placeholder = syncContext.SyncRoot.placeholderList[change.RelativePath];

			if (!placeholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
			{
				var uploadResult = await syncContext.SyncRoot.UploadFileAsync(change.RelativePath, UploadMode.Update, DataProcessingCancellationTokenSource.Token);

				if (!uploadResult.Succeeded)
				{

				}

				modifyResult = uploadResult;
			}
			else
			{
				modifyResult = new(NtStatus.STATUS_UNSUCCESSFUL);
			}

			return modifyResult;
		}

		private async Task<Result> ChangeStateAsync(Change change)
		{
			var placeholder = syncContext.SyncRoot.placeholderList[change.RelativePath];

			// Dehydration requested
			if (placeholder.StandartInfo.PinState.HasFlag(CF_PIN_STATE.CF_PIN_STATE_UNPINNED))
			{
				return await Task.Run(() => placeholder.Dehydrate());
			}

			// Hydration requested
			if (placeholder.StandartInfo.PinState == CF_PIN_STATE.CF_PIN_STATE_PINNED && placeholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
			{
				return await Task.Run(() => placeholder.Hydrate());
			}

			return new Result();
		}
		#endregion

		#region "Remote processing"
		private async Task ProcessRemoteChange(Change change)
		{
			Task task = change.Type switch
			{
				ChangeType.Created => AddLocalAsync(change),
				ChangeType.Deleted => DeleteLocalAsync(change),
				ChangeType.Modified => ModifyLocalAsync(change),
			};

			await task;
		}

		private async Task<Result> AddLocalAsync(Change change)
		{
			throw new NotImplementedException();
		}

		private async Task<Result> DeleteLocalAsync(Change change)
		{
			return await Task.Run(() => syncContext.SyncRoot.DeleteLocal(change.RelativePath));
		}

		private async Task<Result> ModifyLocalAsync(Change change)
		{
			Result modifyResult;

			Placeholder placeholder = syncContext.SyncRoot.placeholderList[change.RelativePath];

			if (!placeholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
			{
				if (placeholder.StandartInfo.PinState.HasFlag(CF_PIN_STATE.CF_PIN_STATE_PINNED))
				{
					//modifyResult = syncContext.SyncRoot.DownloadPinned();
				}
				else
				{
					//modifyResult = syncContext.SyncRoot.DownloadPinned();
				}
			}
			else
			{
				modifyResult = new(NtStatus.STATUS_UNSUCCESSFUL);
			}

			return new Result();
		}



		#endregion
	}
}
