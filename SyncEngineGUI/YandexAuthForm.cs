using Microsoft.Web.WebView2.WinForms;
using System.Text.RegularExpressions;

namespace SyncEngineGUI
{
	public partial class YandexAuthForm : Form
	{
		private readonly Regex regToken = new("access_token=(?<token>[^&]+)", RegexOptions.Compiled);
		
		public string ClientId { get; set; } = string.Empty;
		public string Token { get; set; } = string.Empty;

		public YandexAuthForm()
		{
			InitializeComponent();
			webView21.NavigationCompleted += WebView21_NavigationCompleted!;
		}

		private async void WebView21_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
		{
			var webView = sender as WebView2;

			string url = webView!.Source.ToString();
			if (url.StartsWith("https://oauth.yandex.ru/verification_code"))
			{
				var m = regToken.Match(url);
				if (m.Success)
				{
					Token = m.Groups["token"].Value;
				}

				Task clearCacheTask = ClearCache(webView);
				this.Close();
				await clearCacheTask;
			}
		}

		private void YandexAuthForm_Load(object sender, EventArgs e)
		{
			webView21.Source = new Uri($"https://oauth.yandex.ru/authorize?response_type=token&client_id={ClientId}");
		}

		private static async Task ClearCache(WebView2 webView)
		{
			var profile = webView.CoreWebView2.Profile;
			Task clearCacheTask = profile.ClearBrowsingDataAsync();
			await clearCacheTask;
		}
	}
}
