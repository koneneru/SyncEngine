using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class ReadFileResult : Result
	{
		private readonly int bytesRead;

		public int BytesRead { get {  return bytesRead; } }

		public ReadFileResult() : base() { }

		public ReadFileResult(NtStatus status) : base(status) { }

        public ReadFileResult(Exception ex) : base(ex) { }

        public ReadFileResult(int bytesRead) : base()
		{
			this.bytesRead = bytesRead;
		}
    }
}
