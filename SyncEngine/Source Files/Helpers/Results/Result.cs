using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace SyncEngine
{
	public class Result
	{
		private protected readonly NtStatus _status;
		private protected readonly string _message;

		public bool Succeeded { get { return _status == NtStatus.STATUS_SUCCESS; } }
		public NtStatus Status { get { return _status; } }
		public string Message { get { return _message; } }

		public Result()
		{
			_status = NtStatus.STATUS_SUCCESS;
			_message = _status.ToString();
		}

		public Result(NtStatus status)
		{
			_status = status;
			_message= status.ToString();
		}

        public Result(Exception ex)
        {
			_message = ex.Message;
			_status = ex switch
			{
				FileNotFoundException => NtStatus.STATUS_NOT_A_CLOUD_FILE,
				DirectoryNotFoundException => NtStatus.STATUS_NOT_A_CLOUD_FILE,
				UnauthorizedAccessException => NtStatus.STATUS_CLOUD_FILE_ACCESS_DENIED,
				IOException => NtStatus.STATUS_CLOUD_FILE_IN_USE,
				NotSupportedException => NtStatus.STATUS_CLOUD_FILE_NOT_SUPPORTED,
				InvalidOperationException => NtStatus.STATUS_CLOUD_FILE_INVALID_REQUEST,
				OperationCanceledException => NtStatus.STATUS_CLOUD_FILE_REQUEST_CANCELED,
				_ => NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL
			};
		}

		// Temporary not used... maybe not temporary
		#region "Not used SetException"
		//      public void SetException(Exception exception)
		//{
		//	_succsessful = false;
		//	_message = exception.Message;
		//	_status = exception switch
		//	{
		//		FileNotFoundException => NtStatus.STATUS_NOT_A_CLOUD_FILE,
		//		DirectoryNotFoundException => NtStatus.STATUS_NOT_A_CLOUD_FILE,
		//		UnauthorizedAccessException => NtStatus.STATUS_CLOUD_FILE_ACCESS_DENIED,
		//		IOException => NtStatus.STATUS_CLOUD_FILE_IN_USE,
		//		NotSupportedException => NtStatus.STATUS_CLOUD_FILE_NOT_SUPPORTED,
		//		InvalidOperationException => NtStatus.STATUS_CLOUD_FILE_INVALID_REQUEST,
		//		OperationCanceledException => NtStatus.STATUS_CLOUD_FILE_REQUEST_CANCELED,
		//		_ => NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL
		//	};
		//}
		#endregion
	}
}
