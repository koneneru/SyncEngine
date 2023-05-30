using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Windows.Storage.Provider;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Console;
using WinRT;
using static Vanara.PInvoke.CldApi;

namespace SyncEngine
{
	//===============================================================
	// SyncRootFileSystemWatcher
	//
	//	 This class watches for any hcanges that happen to files
	//	 and folders in the Sync Root on the client machine. This
	//	 allows for hydration to be signalled or other actions.
	//
	// Fakery Factor:
	//
	//   This class is pretty usable as-is. You will probably want to
	//	 get rid of that whole Ctrl+C shenanigans thing to stop the
	//	 watcher and replace it with code that's called by some UI
	//	 you do to uninstall.
	//
	//===============================================================

	internal class SyncRootFileSystemWatcher
	{
		private SyncContext syncContext;
		public FileSystemWatcher fsWatcher;
		private bool shutdownWatcher;
		private StorageProviderState state;
		//private List<EventHandler<IInspectable>> statusChanged;

		public StorageProviderState State { get { return state; } }

		internal SyncRootFileSystemWatcher(in string path, SyncContext context)
		{
			syncContext = context;

			fsWatcher = new FileSystemWatcher
			{
				Path = path,
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName
						| NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size,
				Filter = "*"
			};
			fsWatcher.Created += new FileSystemEventHandler(FileSystemWatcher_OnCreated);
			fsWatcher.Changed += new FileSystemEventHandler(FileSystemWatcher_OnChanged);
			fsWatcher.Deleted += new FileSystemEventHandler(FileSystemWatcher_OnDeleted);
			fsWatcher.Error += new ErrorEventHandler(FileSystemWatcher_OnError);
			fsWatcher.EnableRaisingEvents = true;
		}

		public void WatchAndWait()
		{
			// Main loop - wait for Ctrl+C or our named event to be signaled
			PInvoke.SetConsoleCtrlHandler(Stop, true);

			while (true)
			{
				try
				{
					if (shutdownWatcher)
					{
						fsWatcher.EnableRaisingEvents = false;
						fsWatcher.Dispose();

						break;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("CloudProviderSyncRootWatcher watcher failed.");
					throw;
				}
			}
		}

		public BOOL Stop(uint reason)
		{
			shutdownWatcher = true;
			return true;
		}

		private void FileSystemWatcher_OnCreated(object sender, FileSystemEventArgs e)
		{
			if (e.Name == "." || e.Name == "..") return;

			string relativePath = syncContext.SyncRoot.GetRelativePath(e.FullPath);
			syncContext.SyncRoot.AddPlaceholder(relativePath);

			Change change = new()
			{
				RelativePath = syncContext.SyncRoot.GetRelativePath(e.FullPath),
				Type = ChangeType.Created,
				Time = DateTime.Now,
			};
			syncContext.SyncRoot.dataProcessor.AddLocalChange(change);
		}

		private void FileSystemWatcher_OnChanged(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType != WatcherChangeTypes.Changed) return;
			if (e.Name == "." || e.Name == "..") return;

			string relativePath = syncContext.SyncRoot.GetRelativePath(e.FullPath);

			Change change = new()
			{
				RelativePath = syncContext.SyncRoot.GetRelativePath(e.FullPath),
				Type = ChangeType.Modified,
				Time = DateTime.Now,
			};
			syncContext.SyncRoot.dataProcessor.AddLocalChange(change);

			#region "Old Implementation"
			//var timer = new Stopwatch();
			//timer.Start();
			//state = StorageProviderState.Syncing;

			//Console.WriteLine($"Processig change for {e.FullPath}");

			//var attr = (FILE_FLAGS_AND_ATTRIBUTES)PInvoke.GetFileAttributesW(e.FullPath);
			//if (!attr.HasFlag(FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_DIRECTORY))
			//{
			//	SafeFileHandle placeholder = PInvoke.CreateFileW(e.FullPath, FILE_ACCESS_FLAGS.FILE_READ_DATA, 0, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, 0, null);

			//	long offset = 0;
			//	long length = long.MaxValue;

			//	if (attr.HasFlag(FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_PINNED))
			//	{
			//		Console.WriteLine($"Hydrating file {e.FullPath}");
			//		CfHydratePlaceholder(placeholder);
			//	}
			//	else if (attr.HasFlag(FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_UNPINNED))
			//	{
			//		Console.WriteLine($"Dehydrating file {e.FullPath}");
			//		CfDehydratePlaceholder(placeholder, offset, length, CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE, IntPtr.Zero);
			//	}

			//	// For demonstration purposes, spent at least 3 seconds in the Syncing state.
			//	timer.Stop();
			//	var elapsed = timer.ElapsedMilliseconds;
			//	if (elapsed < 3000)
			//	{
			//		Thread.Sleep((int)(3000 - elapsed));
			//	}

			//	state = StorageProviderState.InSync;
			//}
			#endregion
		}

		private void FileSystemWatcher_OnDeleted(object sender, FileSystemEventArgs e)
		{
			Change change = new()
			{
				RelativePath = syncContext.SyncRoot.GetRelativePath(e.FullPath),
				Type = ChangeType.Deleted,
				Time = DateTime.Now,
			};
			syncContext.SyncRoot.dataProcessor.AddLocalChange(change);
		}

		private void FileSystemWatcher_OnError(object sender, ErrorEventArgs e)
		{

		}
	}
}
