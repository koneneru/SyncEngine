
namespace SyncEngine
{
	//===============================================================
	// ProviderFolderLocations
	//
	//	 Manages the locations of the folders where the syncroot
	//   are "cloud" live.
	//
	// Fakery Factor:
	//
	//   You will likely rewrite all of this. But, look on the bright
	//   side: This is a tiny class that does barely anything.
	//
	//===============================================================

	internal class ProviderFolderLocations
	{
		private static string? s_serverFolder;
		private static string? s_clientFolder;

		public static string GetServerFolder() { return s_serverFolder!; }
		public static string GetClientFolder() { return s_clientFolder!; }

		public static bool Init(string? serverFolder, string? clientFolder)
		{
			s_serverFolder = serverFolder ?? PromptForFolderPath("\"Server in the Fluffy Cloud\" Location");
			if (!string.IsNullOrEmpty(s_serverFolder))
			{
				s_clientFolder = clientFolder ?? PromptForFolderPath("\"Syncroot (Client)\" Location");
			}

			#region "Old Implementation"
			//if (serverFolder != null)
			//{
			//	s_serverFolder = serverFolder;
			//}
			//if (clientFolder != null)
			//{
			//	s_clientFolder = clientFolder;
			//}

			//if (string.IsNullOrEmpty(s_serverFolder))
			//{
			//	s_serverFolder = PromptForFolderPath("\"Server in the Fluffy Cloud\" Location");
			//}
			//if (!string.IsNullOrEmpty(s_serverFolder) && string.IsNullOrEmpty(s_clientFolder))
			//{
			//	s_clientFolder = PromptForFolderPath("\"Syncroot (Client)\" Location");
			//}
			#endregion

			bool result = false;
			if (!string.IsNullOrEmpty(s_serverFolder) && !string.IsNullOrEmpty(s_clientFolder))
			{
				// In case they were passed in params we may need to create the folder.
				// If the folders is already there then these are bening calls.
				Directory.CreateDirectory(s_serverFolder);
				Directory.CreateDirectory(s_clientFolder);
				result = true;
			}
			return result;
		}

		private static string PromptForFolderPath(in string title)
		{
			FolderBrowserDialog selectFolder = new()
			{
				Description = title,
				UseDescriptionForTitle = true
			};

			// Restore last location used.
			var settings = Settings.GetInstance();
			if (settings.HasKey(title))
			{
				var lastLocation = settings.Lookup(title);
				if (Directory.Exists(lastLocation))
				{
					selectFolder.InitialDirectory = lastLocation;
				}
			}

			try
			{
				selectFolder.ShowDialog();
			}
			catch
			{
				return string.Empty;
			}

			// Save the last location
			settings.Insert(title, selectFolder.SelectedPath);

			return selectFolder.SelectedPath;
		}
	}
}
