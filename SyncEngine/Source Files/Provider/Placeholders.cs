using SyncEngine;
using System.Text;
using Windows.Storage.Provider;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using Windows.Win32;

namespace SyncEngine
{
	//===============================================================
	// Placeholders
	//
	//	 Generates placeholders on the client to match what is on
	//	 the server.
	//
	// Fakery Factor:
	//
	//   Lotsa lyin' going on here. Since there is no cloud for this
	//	 sample, the Create method just walks the local folder that's
	//	 identified as the "cloud" and generates the placeholders along
	//	 with some custom states just because.
	//
	//===============================================================

	internal class Placeholders
	{
		public static void Create(
			in string sourcePathStr,
			in string sourceSubDirStr,
			in string destPath)
		{
			try
			{
				WIN32_FIND_DATA findData;
				Kernel32.SafeSearchHandle hFileHandle;
				CF_PLACEHOLDER_CREATE_INFO cloudEntry;

				// Ensure that the source path end in a backslash
				string sourcePath = sourcePathStr.EndsWith('\\') ? sourcePathStr : sourcePathStr + '\\';

				// Ensure that a nonempty subdirectory ends in a backslash
				string sourceSubDir = sourceSubDirStr.EndsWith('\\') ? sourceSubDirStr : sourceSubDirStr + '\\';

				string filePattern = new StringBuilder(sourcePath).Append(sourceSubDir).Append('*').ToString();
				string fullDestPath = new StringBuilder(destPath).Append(sourceSubDir).ToString();

				hFileHandle = Kernel32.FindFirstFileEx(
					filePattern,
					Kernel32.FINDEX_INFO_LEVELS.FindExInfoStandard,
					out findData,
					Kernel32.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
					IntPtr.Zero,
					Kernel32.FIND_FIRST.FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY);

				if (!hFileHandle.IsInvalid)
				{
					do
					{
						if (findData.cFileName == "." || findData.cFileName == "..")
						{
							continue;
						}

						string relativeName = sourceSubDir + findData.cFileName;

						cloudEntry = CreatePlaceholderCreateInfo(findData, relativeName);

						#region "Old Implementation"
						//cloudEntry = new()
						//{
						//	FileIdentity = Marshal.StringToCoTaskMemUni(relativeName),
						//	FileIdentityLength = (uint)((relativeName.Length + 1) * Marshal.SizeOf(relativeName[0])),

						//	RelativeFileName = findData.cFileName,
						//	Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC
						//};
						//cloudEntry.FsMetadata.FileSize = (long)findData.FileSize;
						//cloudEntry.FsMetadata.BasicInfo.FileAttributes = (FileFlagsAndAttributes)findData.dwFileAttributes;
						//cloudEntry.FsMetadata.BasicInfo.CreationTime = findData.ftCreationTime;
						//cloudEntry.FsMetadata.BasicInfo.LastWriteTime = findData.ftLastWriteTime;
						//cloudEntry.FsMetadata.BasicInfo.LastAccessTime = findData.ftLastAccessTime;
						//cloudEntry.FsMetadata.BasicInfo.ChangeTime = findData.ftLastWriteTime;

						//if(findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
						//{
						//	cloudEntry.Flags |= CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_DISABLE_ON_DEMAND_POPULATION;
						//	cloudEntry.FsMetadata.FileSize = 0;
						//}
						#endregion

						try
						{
							Console.WriteLine($"Creating placeholder for {relativeName}");
							CfCreatePlaceholders(fullDestPath, new[] { cloudEntry }, 1, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out uint EntriesProcessed).ThrowIfFailed();
						}
						catch(Exception ex)
						{
							Console.WriteLine($"Failed to create placeholder for {relativeName} with 0x{ex.HResult:X8}");
							// Eating it here lets other files still get a chance. Not Worth crashing the sample, but
							// certanly noteworthy for production code
							continue;
                        }

						try
						{
							var prop = new StorageProviderItemProperty()
							{
								Id = 1,
								Value = "Value1",
								// This icon is just for the sample. You should provide your own branded icon here
								IconResource = "shell32.dll,-44",
							};

							Console.WriteLine($"Applying custom state for {relativeName}");
							Utilities.ApplyCustomStatesToPlaceholderFile(destPath, relativeName, ref prop);

							if(findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
							{
								Create(sourcePath, relativeName, destPath);
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Failed to set custom state on {relativeName} with 0x{ex.HResult:X8}");
							// Eating it here lets other files still get a chance. Not worth crashing the sample, but
							// certanly noteworthy for production code.
						}
					}
					while (Kernel32.FindNextFile(hFileHandle, out findData));

					hFileHandle.Close();
				}
			}
			catch (Exception ex)
			{
                Console.WriteLine($"Could not create cloud file placeholders in the sync root with 0x{ex.HResult:X8}");
				// Something weird enough happened that this is worth crashing out
				throw;
            }
		}

		public static CF_PLACEHOLDER_CREATE_INFO CreatePlaceholderCreateInfo(in WIN32_FIND_DATA findData, in string relativeName)
		{
			CF_PLACEHOLDER_CREATE_INFO info = new()
			{
				FileIdentity = Marshal.StringToCoTaskMemUni(relativeName),
				FileIdentityLength = (uint)(relativeName.Length * Marshal.SizeOf(relativeName[0])),
				RelativeFileName = findData.cFileName,
				Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC,

				FsMetadata = new()
				{
					FileSize = (long)findData.FileSize,
					BasicInfo = CreateBasicInfo(findData)
				}
			};

			if (findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
			{
				info.Flags |= CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_DISABLE_ON_DEMAND_POPULATION;
				info.FsMetadata.FileSize = 0;
			}

			return info;
		}

		public static Kernel32.FILE_BASIC_INFO CreateBasicInfo(in WIN32_FIND_DATA findData)
		{
			return new()
			{
				FileAttributes = (FileFlagsAndAttributes)findData.dwFileAttributes,
				CreationTime = findData.ftCreationTime,
				LastAccessTime = findData.ftLastAccessTime,
				LastWriteTime = findData.ftLastWriteTime,
				ChangeTime = findData.ftLastWriteTime
			};
		}
	}
}
