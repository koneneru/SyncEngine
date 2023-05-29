using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine
{
	public class DataProcessor
	{
		SyncContext syncContext;

		CancellationTokenSource DataProcessingCancellationTokenSource = new();

		public readonly ChangesQueue LocalChanges = new();
		public readonly ChangesQueue RemoteChanges = new();

		private readonly Task ProcessingDataLocalTask;
		private readonly Task ProcessingDataRemoteTask;

		private ActionBlock<Change> ChangesToProcessLocal;
		private ActionBlock<Change> ChangesToProcessRemote;

		public DataProcessor(SyncContext context)
		{
			this.syncContext = context;

			ProcessingDataLocalTask = RunProcessDataLocalTask();
			ProcessingDataRemoteTask = RunProcessDataRemoteTask();

			ChangesToProcessLocal = new(ProcessDataLocal, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = 8,
				CancellationToken = DataProcessingCancellationTokenSource.Token
			});
			ChangesToProcessRemote = new(ProcessDataRemote, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = 8,
				CancellationToken = DataProcessingCancellationTokenSource.Token
			});
		}

		public void AddToChangeOnServerQueue(Change change)
		{
			RemoteChanges.Enqueue(change);
		}

		public void AddLocalChange(Change change)
		{
			if (change.Type != ChangeType.Modified)
			{				
				LocalChanges.Enqueue(change);
			}
			else
			{
				//AddToProcessingQueue(relativePath);
			}
		}

		public void AddLocalChange(string relativePath, ChangeType changeType)
		{
			if (changeType != ChangeType.Modified)
			{
				Change change = new()
				{
					RelativePath = relativePath,
					Type = changeType,
					Time = DateTime.UtcNow,
				};
				LocalChanges.Enqueue(change);
			}
			else
			{
				AddToProcessingQueue(relativePath);
			}
		}

		public async void AddToProcessingQueue(string relativePath)
		{
			var t1 = syncContext.SyncRoot.GetPlaceholderAsync(relativePath);
			var t2 = syncContext.ServerProvider.GetPlaceholderAsync(relativePath);

			AddToProcessingQueue(await t1, await t2);
		}

		/// <summary>
		/// Compairs <paramref name="localPlaceholder"/> and <paramref name="remotePlaceholder"/> and add new Change to
		/// DataToChangeLocal if <paramref name="remotePlaceholder"/> is newer, otherwise to DataToChangeRemote.
		/// <para>If both parameters are null, it does nothing</para>
		/// </summary>
		/// <param name="localPlaceholder"></param>
		/// <param name="remotePlaceholder"></param>
		public void AddToProcessingQueue(FileBasicInfo? localPlaceholder, FileBasicInfo? remotePlaceholder = null)
		{
			// TODO: Case when localPlaceholder is null
			//		 Case when file is deleted

			if (localPlaceholder == null && remotePlaceholder == null) return;

			if (localPlaceholder != null)
			{
				int compareETagResult = localPlaceholder.ETag.CompareTo(remotePlaceholder?.ETag);

				// RemotePlaceholder is newer
				if (compareETagResult < 0)
				{
					Change change = new()
					{
						RelativePath = localPlaceholder.RelativePath,
						Type = ChangeType.Modified,
						Time = DateTime.Now,
					};
					LocalChanges.Enqueue(change);

					return;
				}

				// LocalPlaceholder is newer
				if (compareETagResult > 0)
				{
					if (!localPlaceholder.HasFlag(FileAttributes.Directory) || remotePlaceholder == null)
					{
						Change change = new()
						{
							RelativePath = localPlaceholder.RelativePath,
							Type = remotePlaceholder == null ? ChangeType.Created : ChangeType.Modified,
							Time = DateTime.Now,
						};

						RemoteChanges.Enqueue(change);

						return;
					}
				}

				// ETags are Equal, so it is State change
				
			}
		}

		private Task RunProcessDataLocalTask()
		{
			return Task.Factory.StartNew(async () =>
			{
				while (!DataProcessingCancellationTokenSource.IsCancellationRequested)
				{
					if (RemoteChanges.TryDequeue(out var item))
					{
						try
						{
							if (item.RelativePath == "." || item.RelativePath == "..")
								continue;

							await ProcessDataLocal(item);
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Failed to process changes of {item.RelativePath} with hr 0x{ex.HResult:X8}");
						}
					}
				}
			}, DataProcessingCancellationTokenSource.Token, TaskCreationOptions.LongRunning |
			TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap();
		}

		private Task RunProcessDataRemoteTask()
		{
			return Task.Factory.StartNew(async () =>
			{
				while (!DataProcessingCancellationTokenSource.IsCancellationRequested)
				{
					if (LocalChanges.TryDequeue(out var item))
					{
						try
						{
							if (item.RelativePath == "." || item.RelativePath == "..")
								continue;

							await ChangesToProcessRemote.SendAsync(item);
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Failed to process changes of {item.RelativePath} with hr 0x{ex.HResult:X8}");
						}
					}
				}
			}, DataProcessingCancellationTokenSource.Token, TaskCreationOptions.LongRunning |
			TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap();
		}

		#region "Local processing"
		private async Task ProcessDataLocal(Change change)
		{
			var r = change.Type switch
			{
				ChangeType.Created => await AddLocalAsync(change),
				ChangeType.Deleted => await DeleteLocalAsync(change),
				ChangeType.Modified => await ModifyLocalAsync(change),
				ChangeType.State => await ChangeStateAsync(change)
			};
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

		private async Task<Result> ChangeStateAsync(Change change)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region "Remote processing"
		private async Task ProcessDataRemote(Change change)
		{
			_ = change.Type switch
			{
				ChangeType.Created => await AddToServerAsync(change),
				ChangeType.Deleted => await DeleteFromServerAsync(change),
				ChangeType.Modified => await ModifyOnServerAsync(change),
			};
		}

		private async Task<Result> AddToServerAsync(Change change)
		{
			Result uploadResult;

			string fullPath = syncContext.SyncRoot.GetFullPath(change.RelativePath);

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

			if(!placeholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL))
			{
				var uploadResult = await syncContext.SyncRoot.UploadFileAsync(change.RelativePath, UploadMode.Update, DataProcessingCancellationTokenSource.Token);

				if(!uploadResult.Succeeded)
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
		#endregion
	}
}
