﻿using SyncEngine.ServerProviders;
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
using Microsoft.Win32.SafeHandles;

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
		private readonly int chunckSize = 1024 * 1024 * 2;
		private readonly int stackSize = 1024 * 512;


		private CancellationToken GlobalShutdownToken => GlobalShutdownTokenSource.Token;


		public List<Placeholder> placeholderList = new();

		//DataChangesQueue ToChangeDataOnServerQueue = new();

		//Task LocalChangedDataQueueTask;
		//Task ServerChangedDataQueueTask;

		public SyncRoot(in string rootPath, in IServerProvider cloud)
		{
			localRootFolder = rootPath;
			serverProvider = cloud;

			syncContext = new(this, cloud);
			syncContext.ServerProvider.SyncContext = syncContext;
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
					// This icon is just for the sample. You should provide your own branded icon here
					IconResource = @"%SystemRoot%\system32\imageres.dll,-1043",
					HydrationPolicy = StorageProviderHydrationPolicy.Full,
					HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.None,
					//PopulationPolicy = StorageProviderPopulationPolicy.AlwaysFull,
					PopulationPolicy = StorageProviderPopulationPolicy.Full,
					InSyncPolicy = StorageProviderInSyncPolicy.FileCreationTime | StorageProviderInSyncPolicy.DirectoryCreationTime,
					Version = Application.ProductVersion,
					ShowSiblingsAsGroup = false,
					HardlinkPolicy = StorageProviderHardlinkPolicy.None,

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

		private Task<List<FileBasicInfo>> GetLocalFileList(string subDir, CancellationToken cancellationToken)
		{
			string fullPath = Path.Combine(localRootFolder, subDir);
			List<FileBasicInfo> placeholders = new();

			var directory = new DirectoryInfo(fullPath);
			foreach(var item in directory.EnumerateFileSystemInfos())
			{
				cancellationToken.ThrowIfCancellationRequested();
				placeholders.Add(new FileBasicInfo(fullPath, item));
			}

			return Task.FromResult(placeholders);
		}

		private async Task GetLocalFileListAsync(string subDir, CancellationToken cancellationToken)
		{
			placeholderList.Clear();
			await Task.Run(()=> GetLocalFileListRecursive(subDir), cancellationToken);
		}

		private void GetLocalFileListRecursive(string subDir)
		{
			string fullSubDirPath = Path.Combine(localRootFolder, subDir);

			var directory = new DirectoryInfo(fullSubDirPath);
			foreach (var item in directory.EnumerateFileSystemInfos())
			{
				placeholderList.Add(new Placeholder(fullSubDirPath, item));

				if (item.Attributes.HasFlag(System.IO.FileAttributes.Directory))
				{
					GetLocalFileListRecursive(item.FullName);
				}
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
			return await serverProvider.RemoveAsync(relativePath);
		}

		public async Task<Result> DownloadFileAsync(string relativePath, CancellationToken cancellationToken)
		{
			string fullPath = GetFullPath(relativePath);
			FileStream fs = new(fullPath, FileMode.Open, FileAccess.Write, FileShare.None);
			var fileHandle = fs.SafeFileHandle;

			//_ = serverProvider.DownloadFileAsync(relativePath, fs, cancellationToken);

			//if (!downloadResult.Succeeded)
			//{
			//	Console.WriteLine($"Uploading {relativePath} failed: {downloadResult.Message}");
			//}
			//else
			//{
			//	Console.WriteLine($"Uploading {relativePath} succeed");
			//	var inSyncResult = CfSetInSyncState(fileHandle, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);

			//	if (!inSyncResult.Succeeded)
			//	{
			//		Console.WriteLine($"Failed to set In_Sync_State, with hr 0x{inSyncResult:X8}");
			//	}

			//	//syncContext.SyncRoot.UpdatePlaceholder(uploadResult.Data!, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC);
			//}

			//return downloadResult;

			return new Result();
		}

		public async Task<Result> UploadFileAsync(string relativePath, UploadMode uploadMode, CancellationToken cancellationToken)
		{
			string fullPath = GetFullPath(relativePath);

			FileStream fs = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var fileHandle = fs.SafeFileHandle;

			var uploadResult = await serverProvider.UploadFileAsync(relativePath, fs, uploadMode, cancellationToken);

			if (!uploadResult.Succeeded)
			{
				Console.WriteLine($"Uploading {relativePath} failed: {uploadResult.Message}");
			}
			else
			{
				Console.WriteLine($"Uploading {relativePath} succeed");

				var inSyncResult = CfSetInSyncState(fileHandle, CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);

				if(!inSyncResult.Succeeded)
				{
                    Console.WriteLine($"Failed to set In_Sync_State, with hr 0x{inSyncResult:X8}");
                }

				//syncContext.SyncRoot.UpdatePlaceholder(uploadResult.Data!, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC);
			}
			
			return uploadResult;
		}

		private async Task SynchronizeAsync(string subDir, CancellationToken cancellationToken)
		{
			Task t1 = GetLocalFileListAsync(subDir, cancellationToken);
			Task t2 = serverProvider.GetFileListAsync(subDir, cancellationToken);

			await t1;
			await t2;

			foreach(var placeholder in placeholderList)
			{
				var remoteFileInfo = (from a in serverProvider.FileList where string.Equals(placeholder.RelativeFileName, a.RelativeFileName, StringComparison.CurrentCultureIgnoreCase) select a).FirstOrDefault();

				if (remoteFileInfo != null)
				{
					// File or directory does not exist on server
					if (placeholder.PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC))
					{
						// File or directory has been remotely deleted
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
						// File or directory was locally created or modified
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
					Placeholder.ValidateEtag(placeholder, remoteFileInfo!);

					if (placeholder.StandartInfo.InSyncState.HasFlag(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC))
					{
						if (placeholder.LastWriteTime > remoteFileInfo!.LastWriteTime)
						{
							// Loacal file modified
							Change change = new()
							{
								RelativePath = placeholder.RelativePath,
								Type = ChangeType.Modified,
								Time = DateTime.UtcNow,
							};
							dataProcessor.LocalChanges.Enqueue(change);
						}
						else
						{
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
			foreach(var fileInfo in serverProvider.FileList)
			{
				if(!(from a in placeholderList where string.Equals(fileInfo.RelativeFileName, a.RelativeFileName, StringComparison.CurrentCultureIgnoreCase) select a).Any())
				{
					string baseDir = fileInfo.RelativePath[0..^fileInfo.RelativeFileName.Length];
					var createInfo = fileInfo.ToPlaceholderCreateInfo();
					var result = CfCreatePlaceholders(baseDir, new[] { createInfo }, 1, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out _);
					if(!result.Succeeded)
					{
						Console.WriteLine($"CfCreatePlaceholders for {fileInfo.RelativePath} failed with hr, 0x{result:X8}");
                    }
				}
			}
		}
	}
}
