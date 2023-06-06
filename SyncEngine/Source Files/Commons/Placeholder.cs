using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Win32.Storage.FileSystem;
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

		public Placeholder(string rootPath, string relativePath, WIN32_FIND_DATA findData) : 
			base(relativePath, findData)
		{
			fullPath = Path.Combine(rootPath, relativePath);
			placeholderState = CfGetPlaceholderStateFromFindData(findData);
		}

		public bool ConvertToPlaceholder(bool markInSync = false)
		{
			if (PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
				return true;

			Console.WriteLine($"Converting {RelativePath} to placeholder");

			CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out var fileHandle);

			if (fileHandle.IsInvalid)
			{
				Console.WriteLine($"File handle for {RelativePath} is INVALID");
				CfCloseHandle(fileHandle);
			}
			else
			{
				CF_CONVERT_FLAGS convertFlags = markInSync ? CF_CONVERT_FLAGS.CF_CONVERT_FLAG_MARK_IN_SYNC : CF_CONVERT_FLAGS.CF_CONVERT_FLAG_ENABLE_ON_DEMAND_POPULATION;

				HRESULT result = CfConvertToPlaceholder(fileHandle.DangerousGetHandle(), FileIdentity, FileIdentityLength, convertFlags, out _);

				CfCloseHandle(fileHandle);

				if (!result.Succeeded)
				{
					Console.WriteLine($"Converting {RelativePath} to placeholder FAILED: {result.GetException().Message}");
					return false;
				}
			}

			return true;
		}

		public Result Dehydrate(bool unpin = false)
		{
			if (!PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
				return new Result(NtStatus.STATUS_NOT_A_CLOUD_FILE);
			if (StandartInfo.PinState.HasFlag(CF_PIN_STATE.CF_PIN_STATE_PINNED) && !unpin)
				return new Result(NtStatus.STATUS_CLOUD_FILE_PINNED);

			Console.WriteLine($"Deghydrate placeholder {RelativePath}");

			CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out var fileHandle);
			if (fileHandle.IsInvalid)
			{
				Console.WriteLine($"File handle for {RelativePath} is INVALID");
				CfCloseHandle(fileHandle);

				return new Result(NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL);
			}

			var dehydrateFlag = unpin ? CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE : CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_BACKGROUND;

			HRESULT result = CfDehydratePlaceholder(fileHandle.DangerousGetHandle(), 0, -1, dehydrateFlag);
			CfCloseHandle(fileHandle);

			if (!result.Succeeded)
			{
				Exception ex = result.GetException();
				Console.WriteLine($"Dehydrate placeholder {RelativePath} FAILED: {ex.Message}");
				return new Result(ex);
			}

			return new Result();
		}

		private CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderStandartInfo()
		{
			SafeFileHandle? fileHandle = null;
			CF_PLACEHOLDER_STANDARD_INFO standartInfo = default;
			int bufferLength = 1024;
			IntPtr buffer = IntPtr.Zero;

			if (!PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
				return new CF_PLACEHOLDER_STANDARD_INFO();

			try
			{
				buffer = Marshal.AllocCoTaskMem(bufferLength);

                FILE_ACCESS_FLAGS accessFlag = HasFlag(FileAttributes.Directory) ? FILE_ACCESS_FLAGS.FILE_GENERIC_READ : FILE_ACCESS_FLAGS.FILE_READ_EA;
				FILE_FLAGS_AND_ATTRIBUTES attributsFlag = HasFlag(FileAttributes.Directory) ? FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS : FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED;

				fileHandle = Windows.Win32.PInvoke.CreateFileW(@"\\?\" + fullPath,
					 accessFlag,
					   FILE_SHARE_MODE.FILE_SHARE_READ |
					   FILE_SHARE_MODE.FILE_SHARE_WRITE |
					   FILE_SHARE_MODE.FILE_SHARE_DELETE,
					   null,
					   FILE_CREATION_DISPOSITION.OPEN_EXISTING,
					   attributsFlag,
					   null);

				
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
			CF_PLACEHOLDER_BASIC_INFO basicInfo = default;
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
						basicInfo = Marshal.PtrToStructure<CF_PLACEHOLDER_BASIC_INFO>(buffer);
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

			return basicInfo;
		}

		public Result Hydrate()
		{
			if (!PlaceholderState.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER))
				return new Result(NtStatus.STATUS_NOT_A_CLOUD_FILE);

			Console.WriteLine($"Hydrate placeholder {RelativePath}");

			CfOpenFileWithOplock(fullPath, CF_OPEN_FILE_FLAGS.CF_OPEN_FILE_FLAG_EXCLUSIVE, out var fileHandle);
			if (fileHandle.IsInvalid)
			{
				Console.WriteLine($"File handle for {RelativePath} is INVALID");
				CfCloseHandle(fileHandle);

				return new Result(NtStatus.STATUS_CLOUD_FILE_UNSUCCESSFUL);
			}

			HRESULT result = CfHydratePlaceholder(fileHandle.DangerousGetHandle());
			CfCloseHandle(fileHandle);

			if (!result.Succeeded)
			{
				Exception ex = result.GetException();
				Console.WriteLine($"Hydrate placeholder {RelativePath} FAILED: {ex.Message}");
				return new Result(ex);
			}

			return new Result();
		}

		public async Task<Result> HydrateAsync()
		{
			return await Task.Run(() => Hydrate());
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
            if (placeholder.ETag != secondFile.ETag)
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
