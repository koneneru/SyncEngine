using Microsoft.Win32.SafeHandles;
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
	public class Placeholder : FileBasicInfo
	{
		public readonly string fullPath;
		private CF_PLACEHOLDER_STANDARD_INFO? standartInfo;
		private CF_PLACEHOLDER_STATE placeholderState;

		public CF_PLACEHOLDER_STANDARD_INFO StandartInfo
		{
			get
			{
				standartInfo ??= GetPlaceholderStandartInfo();
				return (CF_PLACEHOLDER_STANDARD_INFO)standartInfo;
			}

			private set { standartInfo = value; }
		}

		public CF_PLACEHOLDER_STATE PlaceholderState { get { return placeholderState; } }

		public CF_PLACEHOLDER_CREATE_INFO CreateInfo { get { return ToPlaceholderCreateInfo(); } }

		public Placeholder(string rootDir, FileSystemInfo info) : base(rootDir, info)
		{
			fullPath = info.FullName;
		}

		public Placeholder(string rootPath, string relativePath, WIN32_FIND_DATA findData) : 
			base(Path.Combine(relativePath, findData.cFileName), findData)
		{
			fullPath = Path.Combine(rootPath, relativePath, findData.cFileName);
			placeholderState = CfGetPlaceholderStateFromFindData(findData);
		}

        private CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderStandartInfo()
		{
			SafeFileHandle? fileHandle = null;
			CF_PLACEHOLDER_STANDARD_INFO standartInfo = default;
			int bufferLength = 1024;
			IntPtr buffer = IntPtr.Zero;

			try
			{
				buffer = Marshal.AllocCoTaskMem(bufferLength);

				using (fileHandle = File.OpenHandle(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, FileOptions.Asynchronous))
				{
					var result = CfGetPlaceholderInfo(fileHandle, CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_STANDARD, buffer, (uint)bufferLength, out uint returnedLength);
					if (returnedLength > 0)
					{
						standartInfo = Marshal.PtrToStructure<CF_PLACEHOLDER_STANDARD_INFO>(buffer);
					}
					else
					{
						Console.WriteLine($"GetPlaceholderStandartInfo Failed with hr 0x{result:X8}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"GetPlaceholderStandartInfo Failed: {ex.Message}");
			}
			finally
			{
				fileHandle?.Close();
				Marshal.FreeCoTaskMem(buffer);
			}

			return standartInfo;
		}

		public CF_PLACEHOLDER_BASIC_INFO GetPlaceholderBasicInfo(FileBasicInfo placeholder)
		{
			SafeFileHandle? fileHandle = null;
			CF_PLACEHOLDER_BASIC_INFO standartInfo = default;
			int bufferLength = 1024;
			IntPtr buffer = IntPtr.Zero;

			try
			{
				buffer = Marshal.AllocCoTaskMem(bufferLength);

				using (fileHandle = File.OpenHandle(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, FileOptions.Asynchronous))
				{
					var result = CfGetPlaceholderInfo(fileHandle, CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_BASIC, buffer, (uint)bufferLength, out uint returnedLength);
					if (returnedLength > 0)
					{
						standartInfo = Marshal.PtrToStructure<CF_PLACEHOLDER_BASIC_INFO>(buffer);
					}
					else
					{
						Console.WriteLine($"GetPlaceholderBasicInf Failed with hr 0x{result:X8}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"GetPlaceholderBasicInf Failed: {ex.Message}");
			}
			finally
			{
				fileHandle?.Close();
				Marshal.FreeCoTaskMem(buffer);
			}

			return standartInfo;
		}

		public void SetInSyncState(CF_IN_SYNC_STATE inSyncState)
		{
			if(StandartInfo.InSyncState == inSyncState) return;

			HRESULT result;
			using(FileStream fs = new(fullPath, FileMode.Open))
			{
				var fileHandle = fs.SafeFileHandle;
				result = CfSetInSyncState(fileHandle, inSyncState, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
			}

			if(result.Succeeded)
			{
				var standartInfo = StandartInfo;
				standartInfo.InSyncState = inSyncState;
				StandartInfo = standartInfo;
			}
			else
			{
				Console.WriteLine($"SetInSyncState for {RelativePath} failed with hr 0x{result:X8}");
            }
		}

		public static void ValidateEtag(Placeholder placeholder, FileBasicInfo secondFile)
		{
			if (placeholder.ETag != secondFile.ETag || placeholder.StandartInfo.ModifiedDataSize > 0)
			{
				placeholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC);
			}
			else
			{
				placeholder.SetInSyncState(CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC);
			}
		}
	}
}
