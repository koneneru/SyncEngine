using SyncEngine;

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

		var returnCode = 0;

		try
		{
			if (FakeCloudProvider.Start())
			{
				returnCode = 1;
			}
		}
		catch
		{
			//CloudProviderSyncRootWatcher.Stop(0); // Param is unsigned
		}

		return returnCode;
    }
}