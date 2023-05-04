using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class FakeCloudProvider
	{
		public static bool Start(in string serverFolder = null, in string clientFolder = null)
		{
			bool result = false;

			if (ProviderFolderLocations.Init(serverFolder, clientFolder))
			{

				result = true;
			}

			return result;
		}
	}
}
