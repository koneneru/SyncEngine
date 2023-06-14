using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine.ServerProviders
{
	public enum ServerProviderStatus
	{
		Disabled = 0x00000000,
		AuthenticationRequired = 0x00000001,
		Connecting = 0x00000002,
		Connected = 0x00000004,
		Disconnected = 0x0000008,

		Failed = 0x7FFFFFFF
	}
}
