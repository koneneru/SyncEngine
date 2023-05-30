using SyncEngine.ServerProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.CldApi.CF_CALLBACK_PARAMETERS.CANCEL;
using static Vanara.PInvoke.CldApi.CF_CALLBACK_PARAMETERS;
using Vanara.PInvoke;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Microsoft.Win32.SafeHandles;
using System.IO;
using Vanara.Extensions;
using Windows.Win32;

namespace SyncEngine
{
	public partial class SyncRoot
	{
		readonly SyncContext syncContext;

		private bool isConnected;
		private readonly string localRootFolder;
		private readonly IServerProvider serverProvider;
		private readonly CancellationTokenSource GlobalShutdownTokenSource = new();
		// This key is used so that the table of callbacks below can be
		// registered/unregistered.
		private CF_CONNECTION_KEY ConnectionKey;
		public DataProcessor dataProcessor;
		SyncRootFileSystemWatcher watcher;
		//private readonly int chunckSize = 1024 * 1024 * 2;
		private readonly int stackSize = 1024 * 512; // Buffer size for P/Invoke Call to CFExecute max 1 MB

		private readonly System.Threading.Timer ResyncTimer;
		private readonly TimeSpan ResyncInterval = TimeSpan.FromSeconds(10);


		private CancellationToken GlobalShutdownToken => GlobalShutdownTokenSource.Token;


		public Dictionary<string, Placeholder> placeholderList = new();

		//DataChangesQueue ToChangeDataOnServerQueue = new();

		//Task LocalChangedDataQueueTask;
		//Task ServerChangedDataQueueTask;

		public SyncRoot(in string rootPath, in IServerProvider cloud)
		{
			localRootFolder = rootPath;
			serverProvider = cloud;

			syncContext = new(this, cloud);
			syncContext.ServerProvider.SyncContext = syncContext;

			ResyncTimer = new(ResyncTimerCallback, null, ResyncInterval, ResyncInterval);
		}

		public async Task Start()
		{
			// Stage 1: Setup
			//--------------------------------------------------------------------------------------------
			// The client folder (syncroot) must be indexed in order for states to properly display
			// НУЖЕН ЛИ?
			//Utilities.AddFolderToSearchIndexer(ProviderFolderLocations.GetClientFolder());
			// Start up the task that registers and hosts the services for the shell (such as custom states, menus, etc)
			/*ShellServices.InitAndStartServiceTask();*/
			// Register the provider with the shell so that the Sync Root shows up in File Explorer
			Register();
			// Hook up callback methods (in this class) for transferring files between client and server
			isConnected = ConnectSyncRootTransferCallbacks();
			// Initiate watcher
			watcher = new SyncRootFileSystemWatcher(localRootFolder , syncContext);
			// Initialize data processor
			dataProcessor = new DataProcessor(syncContext);
			// Connect to Server Provider
			var connectionResult = await serverProvider.Connect();
			if (connectionResult.Succeeded)
			{
                Console.WriteLine($"Connected to {serverProvider.ConnectionString}");
            }
			else
			{
				Console.WriteLine($"Failed to connect to {serverProvider.ConnectionString} with hr 0x{connectionResult.Status:X8}");
            }
			//CfUpdateSyncProviderStatus(ConnectionKey, CF_SYNC_PROVIDERstatus.CF_PROVIDERstatus_IDLE);
			// Create the placeholders in the client folder so the user sees something
			//Placeholders.Create(serverProvider.ConnectionString, string.Empty, localRootFolder);

			// Stage 2: Running
			//--------------------------------------------------------------------------------------------
			// The file watcher loop for this sample will run until the user presses Ctrl+C
			// The file watcher will look for any changes on the files in the client (syncroot) in order
			// to let the cloud know.

			SynchronizeAsync(string.Empty, GlobalShutdownToken);

			watcher.WatchAndWait();

			// Stage 3: Done Running -- caused by Ctrl+C
			//--------------------------------------------------------------------------------------------
			// Unhook up those callback methods
			await Stop();

			// A real sync engine should NOT unregister the sync root upon exit.
			// This is just to demonstrate the use of StorageProviderSyncRootManager.Unregister().
			await Unregister();
		}

		public async Task Stop()
		{
			GlobalShutdownTokenSource.Cancel();
			await serverProvider.Disconnect();

			if (isConnected)
			{
				isConnected = !DisconnectSyncRootTransferCallbacks();
				//CfUpdateSyncProviderStatus(ConnectionKey, CF_SYNC_PROVIDERstatus.CF_PROVIDERstatus_TERMINATED);
			}
		}

		private async Task Register()
		{
			try
			{
				var path = StorageFolder.GetFolderFromPathAsync(localRootFolder);

				StorageProviderSyncRootInfo info = new()
				{
					// This string can be in any form acceptable to SHLoadIndirectString
					DisplayNameResource = "KoneruLocalSyncRoot",
					HardlinkPolicy = StorageProviderHardlinkPolicy.None,
					HydrationPolicy = StorageProviderHydrationPolicy.Partial,
					HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed |
						StorageProviderHydrationPolicyModifier.StreamingAllowed,
					// This icon is just for the sample. You should provide your own branded icon here
					IconResource = @"%SystemRoot%\system32\imageres.dll,-1043",
					//PopulationPolicy = StorageProviderPopulationPolicy.AlwaysFull,
					PopulationPolicy = StorageProviderPopulationPolicy.Full,
					InSyncPolicy = StorageProviderInSyncPolicy.FileLastWriteTime | StorageProviderInSyncPolicy.DirectoryLastWriteTime,
					Version = Application.ProductVersion,
					ShowSiblingsAsGroup = false,

					RecycleBinUri = null,

					// Context
					Context = CryptographicBuffer.ConvertStringToBinary(string.Concat(localRootFolder,
							"->", serverProvider.ConnectionString), BinaryStringEncoding.Utf8),

					Path = await path
				};
				Dictionary<int, string> customStates = new()
				{
					{ 1, "CustomStateName1" },
					{ 2, "CustomStateName2" },
					{ 3, "CustomStateName3" },
				};

				await SyncRootRegistrar.RegisterWithShell(info, customStates);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Could not register the sync root, hr 0x{ex.HResult:X8}");
			}
		}

		private async Task Unregister()
		{
			await Stop();
			SyncRootRegistrar.Unregister(localRootFolder);
		}

		// Register the callbacks in the table at the top of this file so that the methods above
		// are called for our fake provider
		private bool ConnectSyncRootTransferCallbacks()
		{
			s_SECallbackTable = new CF_CALLBACK_REGISTRATION[]
			{
				new CF_CALLBACK_REGISTRATION {
					Callback = new CF_CALLBACK(OnFetchPlaceholders),
					Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS
				},
				new CF_CALLBACK_REGISTRATION {
					Callback = new CF_CALLBACK(OnCancelFetchPlaceholders),
					Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_PLACEHOLDERS
				},
				new CF_CALLBACK_REGISTRATION
				{
					Callback = new CF_CALLBACK(OnFetchData),
					Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_DATA
				},
				new CF_CALLBACK_REGISTRATION
				{
					Callback = new CF_CALLBACK(OnCancelFetchData),
					Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_DATA
				},
				CF_CALLBACK_REGISTRATION.CF_CALLBACK_REGISTRATION_END
			};

			bool result;
			try
			{
				CfConnectSyncRoot(
					localRootFolder,
					s_SECallbackTable,
					IntPtr.Zero,
					CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO |
					CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH,
					out ConnectionKey).ThrowIfFailed();

				result = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Could not connect to sync root, hr 0x{ex.HResult:X8}");
				result = false;
			}

			return result;
		}

		// Unregisters the callbacks in the table at the top of this file so that
		// the client doesn't Hindenburg
		private bool DisconnectSyncRootTransferCallbacks()
		{
			Console.WriteLine("Shutting down");

			bool result;
			try
			{
				CfDisconnectSyncRoot(ConnectionKey).ThrowIfFailed();
				result = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Could not disconnect the sync root, hr 0x{ex.HResult:X8}");
				result= false;
			}

			return result;
		}

		public void AddPlaceholder(string relativePath)
		{
			if(!placeholderList.ContainsKey(relativePath))
			{
				var fileHandle = Kernel32.FindFirstFile(relativePath, out WIN32_FIND_DATA findData);
				if (!fileHandle.IsInvalid)
				{
					placeholderList.Add(relativePath, new Placeholder(localRootFolder, relativePath, findData));
				}
			}
		}

		private async Task LoadFileListAsync(string subDir, CancellationToken cancellationToken)
		{
			await Task.Run(()=> GetLocalFileListRecursive(subDir), cancellationToken);
		}

		private void GetLocalFileListRecursive(string subDir)
		{
			Kernel32.SafeSearchHandle hFileHandle;

			string dirPath = Path.Combine(localRootFolder, subDir);
			string filePattern = Path.Combine(dirPath, "*");

			hFileHandle = Kernel32.FindFirstFileEx(
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
					if (findData.cFileName == "." || findData.cFileName == "..")
						continue;

					string relativePath = Path.Combine(subDir, findData.cFileName);

					if (placeholderList.ContainsKey(relativePath))
					{
						var oldPlaceholer = placeholderList[relativePath];
						var newPlaceholer = new Placeholder(localRootFolder, relativePath, findData);

						if (oldPlaceholer.ETag != newPlaceholer.ETag)
						{
							placeholderList[relativePath] = newPlaceholer;
						}
						else continue;
					}
					else
					{
						placeholderList.Add(relativePath, new Placeholder(localRootFolder, relativePath, findData));
					}

					if (findData.dwFileAttributes.HasFlag(System.IO.FileAttributes.Directory))
					{
						GetLocalFileListRecursive(relativePath);
					}
				}
				while (Kernel32.FindNextFile(hFileHandle, out findData));

				hFileHandle.Close();
			}
		}

		public string GetFullPath(string relativePath)
		{
			return Path.Combine(localRootFolder, relativePath);
		}

		public string GetRelativePath(string fullPath)
		{
			return Path.GetRelativePath(localRootFolder, fullPath);
		}

		public async Task<FileBasicInfo?> GetPlaceholderAsync(string relativePath)
		{
			return await Task.Run(() => GetPlaceholder(relativePath));
		}

		private FileBasicInfo? GetPlaceholder(string relativePath)
		{
			string fullPath = GetFullPath(relativePath);
			if (File.Exists(fullPath))
			{
				FileInfo fileInfo = new(fullPath);
				return new FileBasicInfo(fullPath, fileInfo);
			}
			return default;
		}

		public Result UpdatePlaceholder(FileBasicInfo placeholder, CF_UPDATE_FLAGS cF_UPDATE_FLAGS)
		{
			Result result = new();

			string fullPath = GetFullPath(placeholder.RelativePath);
			var openFileResult = CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out var fileHandle);
			if (openFileResult.Succeeded)
			{
				CF_FILE_RANGE[]? dehydrateRangeArray = null;
				uint dehydrateRangeCount = 0;
				long usn = 0;
				var updateResult = CfUpdatePlaceholder(fileHandle.DangerousGetHandle(), placeholder.FsMetadata, placeholder.FileIdentity,
					placeholder.FileIdentityLength, dehydrateRangeArray, dehydrateRangeCount, cF_UPDATE_FLAGS, ref usn);
				if (!updateResult.Succeeded)
				{
					result = new(updateResult.GetException());
				}
			}
			else
			{
				result = new(openFileResult.GetException());
			}

			if(!result.Succeeded)
			{
				Console.WriteLine($"UpdatePlaceholder failed: {result.Message}");
			}

			return result;
		}

		public Result DeleteLocal(string relativePath)
		{
			Result deletionResult;
			string fullPath = GetFullPath(relativePath);
			try
			{
				if(File.Exists(fullPath))
				{
					File.Delete(fullPath);
				}
				else
				{
					Directory.Delete(fullPath);
				}

				deletionResult = new();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"DeleteLocalAsync failfed: {ex.Message}");
				deletionResult = new(ex);
			}

			return deletionResult;
		}

		public async Task<Result> DeleteRemoteAsync(string relativePath)
		{
			if(placeholderList.Remove(relativePath))
				return await serverProvider.RemoveAsync(relativePath);

			return new Result(NtStatus.STATUS_CLOUD_FILE_INVALID_REQUEST);
		}

		public async Task<Result> UploadFileAsync(string relativePath, UploadMode uploadMode, CancellationToken cancellationToken)
		{
			string fullPath = GetFullPath(relativePath);

			FileStream fs = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			var uploadResult = await serverProvider.UploadFileAsync(relativePath, fs, uploadMode, cancellationToken);
			fs.Close();

			if (!uploadResult.Succeeded)
			{
				Console.WriteLine($"Uploading {relativePath} failed: {uploadResult.Message}");
			}
			else
			{
				Console.WriteLine($"Uploading {relativePath} succeed");

				// Update local Timestamps. Use Server Time to ensure consistent change time.
				var remoteFileInfo = serverProvider.FileList[relativePath];

				watcher.fsWatcher.EnableRaisingEvents = false;

				File.SetCreationTimeUtc(fullPath, remoteFileInfo.CreationTime.ToUniversalTime());
				File.SetLastWriteTimeUtc(fullPath, remoteFileInfo.LastWriteTime.ToUniversalTime());
				File.SetLastAccessTimeUtc(fullPath, remoteFileInfo.LastAccessTime.ToUniversalTime());

				watcher.fsWatcher.EnableRaisingEvents = true;

				FileStream fStream = new(fullPath, FileMode.Open, FileAccess.Write, FileShare.None);

				watcher.fsWatcher.EnableRaisingEvents = false;
				var inSyncResult = CfSetInSyncState(fStream.SafeFileHandle, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
				watcher.fsWatcher.EnableRaisingEvents = true;

				fStream.Close();

				if (!inSyncResult.Succeeded)
				{
					Console.WriteLine($"Failed to set In_Sync_State, with {inSyncResult:X8}");
				}

				ReloadPlaceholder(placeholderList[relativePath]);
			}
			
			return uploadResult;
		}

		private async Task SynchronizeAsync(string subDir, CancellationToken cancellationToken)
		{
			Console.WriteLine($"Start Synchronization for root\\{subDir}");

			Task t1 = LoadFileListAsync(subDir, cancellationToken);
			Task t2 = serverProvider.GetFileListAsync(subDir, cancellationToken);

			await t1;
			await t2;

			foreach(var item in placeholderList)
			{
				var placeholder = item.Value;

				if (!placeholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
				{
					if (placeholder.ConvertToPlaceholder())
					{
						ReloadPlaceholder(placeholder);
					}
					else
					{
						Console.WriteLine($"{placeholder.RelativePath} is not a placeholder");
						continue;
					}
				}

				if (!serverProvider.FileList.ContainsKey(item.Key))
				{
					// File or directory does not exist on server
					if (placeholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
					{
						// File or directory has been remotely deleted
						placeholderList.Remove(item.Key);

						Change change = new()
						{
							RelativePath = placeholder.RelativePath,
							Type = ChangeType.Deleted,
							Time = DateTime.UtcNow,
						};
						dataProcessor.RemoteChanges.Enqueue(change);
					}
					else
					{
						// File or directory was locally created
						Change change = new()
						{
							RelativePath = placeholder.RelativePath,
							Type = ChangeType.Created,
							Time = DateTime.UtcNow,
						};
						dataProcessor.LocalChanges.Enqueue(change);
					}
				}
				else
				{
					// File or directory exists on server
					var remoteFileInfo = serverProvider.FileList[item.Key];

					Placeholder.ValidateEtag(placeholder, remoteFileInfo);

					if (placeholder.StandartInfo.InSyncState == CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC)
					{
						if (placeholder.LastWriteTime > remoteFileInfo.LastWriteTime)
						{
							// File modified loacally
							if (placeholder.PlaceholderState == CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_NO_STATES)
							{
								// Placeholder just created, should fix metadata
								Change change = new()
								{
									RelativePath = placeholder.RelativePath,
									Type = ChangeType.State,
									Time = DateTime.UtcNow,
								};
								dataProcessor.RemoteChanges.Enqueue(change);
							}
							else
							{
								// File really was modified locally
								Change change = new()
								{
									RelativePath = placeholder.RelativePath,
									Type = ChangeType.Modified,
									Time = DateTime.UtcNow,
								};
								dataProcessor.LocalChanges.Enqueue(change);
							}

						}
						else
						{
							// File modified remotely
							Change change = new()
							{
								RelativePath = placeholder.RelativePath,
								Type = ChangeType.Modified,
								Time = DateTime.UtcNow,
							};
							dataProcessor.RemoteChanges.Enqueue(change);
						}
					}
				}
			}

			// Add missing placeholders
			foreach(var item in serverProvider.FileList)
			{
				var fileInfo = item.Value;

				if (!placeholderList.ContainsKey(item.Key))
				{
					//string baseDir = fileInfo.RelativePath[0..^fileInfo.RelativeFileName.Length];
					string baseDir = item.Key[0..^fileInfo.RelativeFileName.Length];
					string fullDestPath = Path.Combine(localRootFolder, baseDir);
					var createInfo = fileInfo.ToPlaceholderCreateInfo();
					var result = CfCreatePlaceholders(fullDestPath, new[] { createInfo }, 1, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out _);
					if (!result.Succeeded)
					{
						Console.WriteLine($"CfCreatePlaceholders for {fileInfo.RelativePath} failed with {result}");
                    }
				}
			}
		}

		private async void ResyncTimerCallback(object sender)
		{
			if (!isConnected) return;

			ResyncTimer.Change(Timeout.Infinite, Timeout.Infinite);

			try
			{
				await SynchronizeAsync(string.Empty, GlobalShutdownToken);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Resync for root failed: {ex.Message}");
			}
			finally
			{
				ResyncTimer.Change(ResyncInterval, ResyncInterval);
			}
		}

		private void ReloadPlaceholder(Placeholder placeholder)
		{
			var fileHandle = Kernel32.FindFirstFile(placeholder.fullPath, out WIN32_FIND_DATA findData);
			try
			{
				placeholderList[placeholder.RelativePath] = new Placeholder(localRootFolder, placeholder.RelativePath, findData);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed reload placeholder {placeholder.RelativePath}: {ex.Message}");
			}
			finally
			{
				fileHandle?.Close();
			}
		}
	}
}
