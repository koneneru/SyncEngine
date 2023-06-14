using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class SyncContext
	{
		public SyncRoot SyncRoot;
		public IServerProvider ServerProvider;

        public SyncContext(SyncRoot root, IServerProvider provider)
        {
            SyncRoot = root;
            ServerProvider = provider;
        }
    }
}
