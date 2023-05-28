using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class OpenFileResult : Result
	{
		//private Placeholder _placeholder;

		public OpenFileResult() { }

		public OpenFileResult(NtStatus status) : base(status) { }

		public OpenFileResult(Exception ex) : base(ex) { }
    }
}
