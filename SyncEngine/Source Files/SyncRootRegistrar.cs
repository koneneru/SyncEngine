using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;

namespace SyncEngine
{
	public class SyncRootRegistrar
	{
		public static List<SyncRootInfo> GetRootList()
		{
			List<SyncRootInfo> list = new();

			var roots = StorageProviderSyncRootManager.GetCurrentSyncRoots();
			foreach (var root in roots)
			{
				if (root.DisplayNameResource.StartsWith("SyncEngine"))
				{
					//StorageProviderSyncRootManager.Unregister(root.Id);
					list.Add(GetSyncRootInfo(root));
				}
			}

			return list;
		}

		public static async Task<SyncRootInfo> Register(string localRootFolder, string providerName, string accessToken)
		{
			var path = StorageFolder.GetFolderFromPathAsync(localRootFolder);

			StorageProviderSyncRootInfo info = new()
			{
				Id = GetSyncRootId(localRootFolder, providerName, accessToken),
				//Id = GetSyncRootId(localRootFolder),
				// This string can be in any form acceptable to SHLoadIndirectString
				DisplayNameResource = $"SyncEngine - {accessToken}",
				HardlinkPolicy = StorageProviderHardlinkPolicy.None,
				HydrationPolicy = StorageProviderHydrationPolicy.Partial,
				HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed |
					StorageProviderHydrationPolicyModifier.StreamingAllowed,
				IconResource = @"%SystemRoot%\system32\imageres.dll,-1043",
				PopulationPolicy = StorageProviderPopulationPolicy.Full,
				InSyncPolicy = StorageProviderInSyncPolicy.FileLastWriteTime | StorageProviderInSyncPolicy.DirectoryLastWriteTime,
				Version = Application.ProductVersion,
				ShowSiblingsAsGroup = false,

				RecycleBinUri = null,

				// Context
				Context = CryptographicBuffer.ConvertStringToBinary(string.Concat(localRootFolder,
						"->", accessToken), BinaryStringEncoding.Utf8),

				Path = await path
			};

			StorageProviderSyncRootManager.Register(info);

			// Give cache some time to invalidate
			await Task.Delay(1000);

			return GetSyncRootInfo(info);
		}

		public static void Unregister(in string id)
		{
			try
			{
				StorageProviderSyncRootManager.Unregister(id);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Could not unregister the sync root, hr 0x{ex.HResult:X8}");
			}
		}

		private static string GetSyncRootId(in string rootFolderPath, in string providerName, in string accessToken)
		{
			var syncRootID = new StringBuilder("SyncEngine");
			syncRootID.Append('!');
			syncRootID.Append(System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value);
			syncRootID.Append('!');
			syncRootID.Append(providerName);
			syncRootID.Append('!');
			if (accessToken.Contains('\\')) syncRootID.Append(Hasher.Hash(rootFolderPath));
			else syncRootID.Append(accessToken);

			return syncRootID.ToString();
		}

		private static void AddCustomState(ref StorageProviderSyncRootInfo info, in KeyValuePair<int, string> state)
		{
			StorageProviderItemPropertyDefinition customState = new()
			{
				Id = state.Key,
				DisplayNameResource = state.Value,
			};
			info.StorageProviderItemPropertyDefinitions.Add(customState);
		}

		private static SyncRootInfo GetSyncRootInfo(StorageProviderSyncRootInfo info)
		{
			var id = info.Id.Split('!');
			return new()
			{
				Id = info.Id,
				Name = info.DisplayNameResource,
				Path = info.Path.Path,
				Provider = id[^2],
				Token = id[^1]
			};
		}
	}
}
