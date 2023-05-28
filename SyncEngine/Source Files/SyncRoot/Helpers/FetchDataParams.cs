using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine
{
	public class FetchDataParams
	{
		public long FileOffset;
		public long Length;
		public string RelativePath;
		public CF_TRANSFER_KEY TransferKey;
		public CF_REQUEST_KEY RequestKey;
		public byte PriorityHint;
	}
}
