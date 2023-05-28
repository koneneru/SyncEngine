using Windows.Storage.Provider;

namespace SyncEngine
{
	public class HardDeleter
	{
		public static void DeleteSyncRoot()
		{
			var roots = StorageProviderSyncRootManager.GetCurrentSyncRoots();
			foreach (var root in roots)
			{
				if(!root.DisplayNameResource.Contains("OneDrive"))
					StorageProviderSyncRootManager.Unregister(root.Id);
			}		
		}
	}
}
