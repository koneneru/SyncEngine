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
				//NotifyFilter = NotifyFilters.Attributes,
				NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName
						| NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size,
				Filter = "*"
			};
			fsWatcher.Created += new FileSystemEventHandler(FileSystemWatcher_OnChanged);
			fsWatcher.Changed += new FileSystemEventHandler(FileSystemWatcher_OnChanged);
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

		private void FileSystemWatcher_OnChanged(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType != WatcherChangeTypes.Changed) return;
			if (e.Name == "." || e.Name == "..") return;

			Change change = new()
			{
				RelativePath = syncContext.SyncRoot.GetRelativePath(e.FullPath),
				Type = ChangeType.Modified
			};
			syncContext.SyncRoot.dataProcessor.AddLocalChange(change);
		}

		private void FileSystemWatcher_OnError(object sender, ErrorEventArgs e)
		{

		}
	}
}
