using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine
{
	public class CloudApiHelper
	{
		public static CF_OPERATION_INFO CreateOperationInfo(in CF_CALLBACK_INFO callbackInfo, in CF_OPERATION_TYPE operationType)
		{
			CF_OPERATION_INFO opInfo = new()
			{
				Type = operationType,
				ConnectionKey = callbackInfo.ConnectionKey,
				TransferKey = callbackInfo.TransferKey,
				CorrelationVector = callbackInfo.CorrelationVector,
				RequestKey = callbackInfo.RequestKey,
			};

			opInfo.StructSize = (uint)Marshal.SizeOf(opInfo);
			return opInfo;
		}
	}
}
