using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SyncEngine
{
	public class ProviderFolderLocations
	{
		private static string? s_serverFolder;
		private static string? s_clientFolder;

		public static string GetServerFolder() { return s_serverFolder!; }
		public static string GetClientFolder() { return s_clientFolder!; }

		public static bool Init(string serverFolder, string clientFolder)
		{
			if (serverFolder != null)
			{
				s_serverFolder = serverFolder;
			}
			if (clientFolder != null)
			{
				s_clientFolder = clientFolder;
			}

			if (string.IsNullOrEmpty(s_serverFolder))
			{
				s_serverFolder = PromptForFolderPath("\"Server in the Fluffy Cloud\" Location");
			}
			if (!string.IsNullOrEmpty(s_serverFolder) && string.IsNullOrEmpty(s_clientFolder))
			{
				s_clientFolder = PromptForFolderPath("\"Syncroot (Client)\" Location");
			}

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
			var settings = Settings.Instance();
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

			settings.Insert(title, selectFolder.SelectedPath);

			return selectFolder.SelectedPath;
		}
	}
}
