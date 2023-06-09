﻿using System.Text.Json;

namespace SyncEngine
{
	internal class Settings
	{
		private readonly string _path;

		private static Settings? instance;

		private Dictionary<string, string> settings;

		private Settings()
		{
			_path = Path.Combine(Application.LocalUserAppDataPath, "settings.json");

			if (!File.Exists(_path))
			{
				CreateSettingsFile();
			}

			try
			{
				settings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_path))!;
			}
			catch
			{
				settings = new Dictionary<string, string>();
				UpdateSettingsFile();
			}
		}

		public static Settings GetInstance()
		{
			return instance ??= new Settings();
		}

		public bool HasKey(string key) { return settings.ContainsKey(key); }

		public string Lookup(string key) { return settings[key]; }

		public void Insert(string key, string value)
		{
			if (settings.ContainsKey(key))
				settings[key] = value;
			else
				settings.Add(key, value);

			UpdateSettingsFile();
		}

		private void CreateSettingsFile()
		{
			File.Create(_path).Close();
			UpdateSettingsFile();
		}

		private void UpdateSettingsFile()
		{
			settings ??= new Dictionary<string, string>();
			var options = new JsonSerializerOptions { WriteIndented = true };
			var json = JsonSerializer.Serialize(settings, options);
			File.WriteAllText(_path, json);
		}
	}
}
