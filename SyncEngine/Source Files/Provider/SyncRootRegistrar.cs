using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;

namespace SyncEngine
{
	internal class SyncRootRegistrar
	{
		public static async Task RegisterWithShell(StorageProviderSyncRootInfo info, Dictionary<int, string>? customStates = null)
		{
			info.Id = GetSyncRootId(info.Path.Path) ;

			if (customStates != null)
			{
				foreach (var state in customStates)
				{
					AddCustomState(ref info, state);
				}
			}

			StorageProviderSyncRootManager.Register(info);

			// Give cache some time to invalidate
			await Task.Delay(1000);
		}

		public static void Unregister(in string rootFolderPath)
		{
			try
			{
				StorageProviderSyncRootManager.Unregister(GetSyncRootId(rootFolderPath));
			}
			catch (Exception ex)
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

		private static string GetSyncRootId(in string rootFolderPath)
		{
			var syncRootID = new StringBuilder(GetAsseblyGUID());
			syncRootID.Append('!');
			syncRootID.Append(System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value);
			syncRootID.Append('!');
			syncRootID.Append(Hasher.Hash(rootFolderPath));

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
	}
}
