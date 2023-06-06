namespace SyncEngineGUI
{
	internal static class Program
	{
		public static ApplicationContext Context { get; set; }

		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();

			Context = new ApplicationContext(new AddProviderForm());
			Application.Run(Context);
		}
	}
}