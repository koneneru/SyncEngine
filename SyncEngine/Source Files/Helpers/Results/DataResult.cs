using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class DataResult<T> : Result
	{
		private readonly T? _data;

        public T? Data { get { return _data; } }

        public DataResult() : base() { }

        public DataResult(NtStatus status) : base(status) { }

        public DataResult(Exception ex) : base(ex) { }

        public DataResult(in T data) : base()
        {
            _data = data;
        }
    }
}
