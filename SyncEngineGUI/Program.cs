using SyncEngine;
using SyncEngine.ServerProviders;

namespace SyncEngineGUI
{
	internal static class Program
	{
		public static ApplicationContext Context { get; set; }
		public static List<SyncRoot> Roots { get; set; } = new List<SyncRoot>();

		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			ApplicationConfiguration.Initialize();

			List<Task> regTasks = new();

			var rootsInfo = SyncRootRegistrar.GetRootList();
			foreach (var item in rootsInfo)
			{
				IServerProvider provider = item.Provider switch
				{
					"Local" => new LocalServerProvider(item.Token),
					"Yandex" => new YandexServerProvider(item.Token),
					_ => throw new NotSupportedException()
				};
				SyncRoot root = new(item, provider);
				Roots.Add(root);
				regTasks.Add(root.Start());
			}

			Context = new ApplicationContext(new MainForm());
			Application.Run();

			Task.WaitAll(regTasks.ToArray());
		}
	}
}