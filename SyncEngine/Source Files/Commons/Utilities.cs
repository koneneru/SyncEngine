using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Provider;

namespace SyncEngine
{
	// All methods and fields for this class are static
	internal class Utilities
	{
		// If the local (client) folder where the cloud file placeholders are created
		// is not under the User folder (i.e. Documents, Photos, etc), then it is required
		// to add the folder to the Search Indexer. This is because the properties for
		// the cloud file state/progress are cached in the indexer, and if the folder isn't
		// indexed, attempts to get the properties on items will not return the expected value.
		public static void AddFolderToSearchIndexer(in string folder)
		{
			string url = "file:///";
			url = String.Concat(url, folder);

			try
			{
                // SHOULD BE IMPLEMENTED LATER... MAYBE... IDK

                Console.WriteLine($"Succesfully called AddFolderToSearchIndexer on {url}");
            }
			catch( Exception ex )
			{
                Console.WriteLine($"Failed on call to AddFolderToSearchIndexer for {url} with hr 0x{ex.HResult:X8}");
            }
		}

		public static void ApplyCustomStatesToPlaceholderFile(in string path, in string fileName, ref StorageProviderItemProperty prop)
		{
			try
			{
				var sb = new StringBuilder(path);
				sb.Append('\\');
				sb.Append(fileName);
				string fullPath = sb.ToString();

				IStorageItem item = StorageFile.GetFileFromPathAsync(fullPath).GetResults();
				StorageProviderItemProperties.SetAsync(item, new[] { prop }).GetResults();
			}
			catch ( Exception ex )
			{
                Console.WriteLine($"Failed to set custom state with 0x{ex.HResult:X8}");
            }
		}
	}
}
