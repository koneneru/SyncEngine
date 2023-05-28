using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;

namespace SyncEngine
{
	//===============================================================
	// CloudProviderRegistrar
	//
	//	 This class registers the provider with the Shell so that
	//   the syncroot shows up.
	//
	// Fakery Factor:
	//
	//   You shold be able to replace the strings with your real values
	//   and then use this class as-is.
	//
	//===============================================================

	internal class CloudProviderRegistrar
	{
		public static async Task RegisterWithShell()
		{
			try
			{
				StorageProviderSyncRootInfo info = new()
				{
					Id = GetSyncRootId(),
					Path = await StorageFolder.GetFolderFromPathAsync(ProviderFolderLocations.GetClientFolder()),
					// This string can be in any form acceptable to SHLoadIndirectString
					DisplayNameResource = "KoneruSyncRoot",
					// This icon is just for the sample. You should provide your own branded icon here
					IconResource = @"%SystemRoot%\system32\charmap.exe,0",
					HydrationPolicy = StorageProviderHydrationPolicy.Full,
					HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.None,
					PopulationPolicy = StorageProviderPopulationPolicy.AlwaysFull,
					InSyncPolicy = StorageProviderInSyncPolicy.FileCreationTime | StorageProviderInSyncPolicy.DirectoryCreationTime,
					Version = Application.ProductVersion,
					ShowSiblingsAsGroup = false,
					HardlinkPolicy = StorageProviderHardlinkPolicy.None,

					RecycleBinUri = null,

					// Context
					Context = CryptographicBuffer.ConvertStringToBinary(string.Concat(ProviderFolderLocations.GetClientFolder(),
						"->", ProviderFolderLocations.GetClientFolder()), BinaryStringEncoding.Utf8),
				};
				AddCustomState(ref info, "CustomStateName1", 1);
				AddCustomState(ref info, "CustomStateName2", 2);
				AddCustomState(ref info, "CustomStateName3", 3);

				StorageProviderSyncRootManager.Register(info);

				// Give cache some time to invalidate
				await Task.Delay(1000);
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Could not register the sync root, hr 0x{ex.HResult:X8}");
            }
		}

		// A real sync engine should NOT unregister the sync root upon exit.
		// This is just to demonstrate the use of StorageProviderSyncRootManager.Unregister().
		public static void Unregister()
		{
			try
			{
				StorageProviderSyncRootManager.Unregister(GetSyncRootId());
			}
			catch(Exception ex)
			{
                Console.WriteLine($"Could not unregister the sync root, hr 0x{ex.HResult:X8}");
            }
		}

		private static string GetAsseblyGUID()
		{
			var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<GuidAttribute>();
			string id = attribute?.Value ?? string.Empty;

			return id;
		}

		private static string GetSyncRootId()
		{
			var syncRootID = new StringBuilder(GetAsseblyGUID());
			syncRootID.Append('!');
			syncRootID.Append(System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value);
			syncRootID.Append('!');
			syncRootID.Append(ProviderFolderLocations.GetClientFolder().GetHashCode());

			return syncRootID.ToString();
		}

		private static void AddCustomState(ref StorageProviderSyncRootInfo info, in string displayNameResource, in int id)
		{
			var customState = new StorageProviderItemPropertyDefinition()
			{
				DisplayNameResource = displayNameResource,
				Id = id,
			};
			info.StorageProviderItemPropertyDefinitions.Add(customState);
		}
	}
}
