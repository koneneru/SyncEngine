using SyncEngine;
using System.Reflection;
using System.Text.RegularExpressions;
using Vanara.PInvoke;
using System.Runtime.InteropServices;
using System.Text;
using SyncEngine.ServerProviders;

public class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		// Detect a common debugging error up front.
		try
		{
			// If the program was launched incorrectly, this will throw.
			//File.Open(Application.LocalUserAppDataPath, FileMode.Open);
			Directory.Exists(Application.LocalUserAppDataPath);
			Console.WriteLine(Application.LocalUserAppDataPath);
		}
		catch
		{
			Console.WriteLine("This program should be launched from the Start menu, not form Visual Studio.");
			return 1;
		}

		Console.WriteLine("Press ctrl+C to stop gracefully");
		Console.WriteLine("-------------------------------");

		HardDeleter.DeleteSyncRoot();

		var returnCode = 0;

		try
		{
			//var cloudProvider = new FakeCloudProvider();
			//if (cloudProvider.Start("E:\\folders\\server", "E:\\folders\\client"))
			//{
			//	returnCode = 1;
			//}

			LocalServerProvider localServerProvider = new("E:\\folders\\server");
			SyncRoot syncRoot = new("E:\\folders\\client", localServerProvider);
			syncRoot.Start();
		}
		catch
		{
			//CloudProviderSyncRootWatcher.Stop(0); // Param is unsigned
		}

		return returnCode;
    }
}