using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.Extensions;
using Vanara.PInvoke;
using Windows.Win32.Storage.CloudFilters;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine
{
	public class FileBasicInfo
	{
		public string RelativePath;
		public IntPtr FileIdentity;
		public uint FileIdentityLength;
		public string RelativeFileName;
		public long FileSize;
		public FileAttributes FileAttributes;
		public DateTime CreationTime;
		public DateTime LastAccessTime;
		public DateTime LastWriteTime;
		public DateTime ChangeTime;
		public string ETag;

		public CF_FS_METADATA FsMetadata => new()
		{
			FileSize = FileSize,
			BasicInfo = CreateBasicInfo()
		};

        public FileBasicInfo()
        {
            
        }

        public FileBasicInfo(string relativePath, WIN32_FIND_DATA findData)
        {
			RelativePath = relativePath;
			FileIdentity = Marshal.StringToCoTaskMemUni(RelativePath);
			FileIdentityLength = (uint)(RelativePath.Length * Marshal.SizeOf(RelativePath[0]));
			RelativeFileName = findData.cFileName;
			FileSize = (long)findData.FileSize;
			FileAttributes = findData.dwFileAttributes;
			CreationTime = findData.ftCreationTime.ToDateTime().ToUniversalTime();
			LastAccessTime = findData.ftLastAccessTime.ToDateTime().ToUniversalTime();
			LastWriteTime = findData.ftLastWriteTime.ToDateTime().ToUniversalTime();
			ChangeTime = findData.ftLastWriteTime.ToDateTime().ToUniversalTime();
			ETag = new StringBuilder('_')
				.Append(LastWriteTime.ToUniversalTime().Ticks)
				.Append('_')
				.Append(FileSize).ToString();
        }

		public FileBasicInfo(string rootDir, FileSystemInfo info)
		{
			RelativePath = Path.GetRelativePath(rootDir, info.FullName);
			FileIdentity = Marshal.StringToCoTaskMemUni(RelativePath);
			FileIdentityLength = (uint)(RelativePath.Length * Marshal.SizeOf(RelativePath[0]));
			RelativeFileName = info.Name;
			FileSize = info.Attributes.HasFlag(FileAttributes.Directory) ? 0 : ((FileInfo)info).Length;
			FileAttributes = info.Attributes;
			CreationTime = info.CreationTime;
			LastAccessTime = info.LastAccessTime;
			LastWriteTime = info.LastWriteTime;
			ChangeTime = info.LastWriteTime;
			ETag = new StringBuilder('_')
				.Append(LastWriteTime.ToUniversalTime().Ticks)
				.Append('_')
				.Append(FileSize).ToString();
		}

		public CF_PLACEHOLDER_CREATE_INFO ToPlaceholderCreateInfo()
		{
			CF_PLACEHOLDER_CREATE_INFO info = new()
			{
				FileIdentity = Marshal.StringToCoTaskMemUni(RelativePath),
				FileIdentityLength = (uint)(RelativePath.Length * Marshal.SizeOf(RelativePath[0])),
				RelativeFileName = RelativeFileName,
				Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC,

				FsMetadata = new()
				{
					FileSize = (long)FileSize,
					BasicInfo = CreateBasicInfo()
				}
			};

			if (FileAttributes.HasFlag(FileAttributes.Directory))
			{
				info.Flags |= CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_DISABLE_ON_DEMAND_POPULATION;
				info.FsMetadata.FileSize = 0;
			}

			return info;
		}

		private Kernel32.FILE_BASIC_INFO CreateBasicInfo()
		{
			return new()
			{
				FileAttributes = (FileFlagsAndAttributes)FileAttributes,
				CreationTime = CreationTime.ToFileTimeStruct(),
				LastAccessTime = LastAccessTime.ToFileTimeStruct(),
				LastWriteTime = LastWriteTime.ToFileTimeStruct(),
				ChangeTime = LastWriteTime.ToFileTimeStruct()
			};
		}

		public bool HasFlag(FileAttributes attributes)
		{
			return FileAttributes.HasFlag(attributes);
		}

		public bool IsEqual(FileBasicInfo placeholder)
		{
			return this.ETag == placeholder.ETag;
		}
	}
}
