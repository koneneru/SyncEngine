using SyncEngine;
using SyncEngine.ServerProviders;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine
{
	//===============================================================
	// FakeCloudProvider
	//
	//	 This is the top-level class that implements our fake
	//   cloud provider. It's the entry point ("Start") and
	//   the different facets of implementing a cloud provider
	//   are implemented in a bunch of helper classes. This
	//   hepls keep the intent of each class crisp for easier
	//   digestion.
	//
	// Fakery Factor:
	//
	//   Most of this is usable as-is. You would want to avoid using
	//   the FileCopierWithProgress class and replace that with
	//   your own code that brights stuff down from a real cloud
	//   server and stores in on the client.
	//
	//   And the shutdown story will be different.
	//
	//===============================================================

	public class FakeCloudProvider
	{
		// This key is used so that the table of callbacks below can be
		// registered/unregistered.
		private CF_CONNECTION_KEY transferCallbackConnectionKey;

		private static CF_CALLBACK_REGISTRATION[] s_SECallbackTable = new CF_CALLBACK_REGISTRATION[]
		{
			new CF_CALLBACK_REGISTRATION
			{
				Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_DATA,
				Callback = new CF_CALLBACK(OnFetchData)
			},
			new CF_CALLBACK_REGISTRATION
			{
				Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_DATA,
				Callback = new CF_CALLBACK(OnCancelFetchData)
			},
			CF_CALLBACK_REGISTRATION.CF_CALLBACK_REGISTRATION_END
		};

		// Starts the Fake Cloud Provider. Returns when you press CTRL+C in the console window.
		public bool Start(in string? serverFolder = null, in string? clientFolder = null)
		{
			bool result = false;

			if (ProviderFolderLocations.Init(serverFolder, clientFolder))
			{
				// Stage 1: Setup
				//--------------------------------------------------------------------------------------------
				// The client folder (syncroot) must be indexed in order for states to properly display
				// НУЖЕН ЛИ?
				Utilities.AddFolderToSearchIndexer(ProviderFolderLocations.GetClientFolder());
				// Start up the task that registers and hosts the services for the shell (such as custom states, menus, etc)
				/*ShellServices.InitAndStartServiceTask();*/
				// Register the provider with the shell so that the Sync Root shows up in File Explorer
				CloudProviderRegistrar.RegisterWithShell();
				// Hook up callback methods (in this class) for transferring files between client and server
				ConnectSyncRootTransferCallbacks();
				// Create the placeholders in the client folder so the user sees something
				Placeholders.Create(ProviderFolderLocations.GetServerFolder(), string.Empty, ProviderFolderLocations.GetClientFolder());

				// Stage 2: Running
				//--------------------------------------------------------------------------------------------
				// The file watcher loop for this sample will run until the user presses Ctrl+C
				// The file watcher will look for any changes on the files in the client (syncroot) in order
				// to let the cloud know.
				//var watcher = new SyncRootFileSystemWatcher(ProviderFolderLocations.GetClientFolder());
				//watcher.WatchAndWait();

				// Stage 3: Done Running-- caused by Ctrl+C
				//--------------------------------------------------------------------------------------------
				// Unhook up those callback methods
				DisconnectSyncRootTransferCallbacks();

				// A real sync engine should NOT unregister the sync root upon exit.
				// This is just to demonstrate the use of StorageProviderSyncRootManager.Unregister().
				CloudProviderRegistrar.Unregister();

				// And if we got here, then this was a normally run test versus crash-o-rama
				result = true;
			}

			return result;
		}

		// When the client needds to fetch data from the cloud, this method will be called.
		// The FileCopierWithProgress class does the actual work of copying files from
		// the "cloud" to the "client" and updating the transfer status along theway.
		private static void OnFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
			FileCopierWithProgress.CopyFromServerToClient(callbackInfo, callbackParameters, ProviderFolderLocations.GetServerFolder());
		}

		// When the fetch is cancelled, this happens. Our FileCopierWithProgress doesn't really care, because
		// it's fake.
		private static void OnCancelFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
			FileCopierWithProgress.CancelCopyFromServerToClient(callbackInfo, callbackParameters);
		}

		// Register the callbacks in the table at the top of this file so that the methods above
		// are called for our fake provider
		private void ConnectSyncRootTransferCallbacks()
		{
			try
			{
				CfConnectSyncRoot(
					ProviderFolderLocations.GetClientFolder(),
					s_SECallbackTable,
					IntPtr.Zero,
					CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO |
					CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH,
					out transferCallbackConnectionKey).ThrowIfFailed();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Could not connect to sync root, hr 0x{ex.HResult:X8}");
			}
		}

		// Unregisters the callbacks in the table at the top of this file so that
		// the client doesn't Hindenburg
		private void DisconnectSyncRootTransferCallbacks()
		{
            Console.WriteLine("Shutting down");

			try
			{
				CfDisconnectSyncRoot(transferCallbackConnectionKey).ThrowIfFailed();
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Could not disconnect the sync root, hr 0x{ex.HResult:X8}");
			}
		}
	}
}
