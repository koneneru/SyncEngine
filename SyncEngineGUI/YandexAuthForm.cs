using Microsoft.Web.WebView2.WinForms;
using System.Text.RegularExpressions;

namespace SyncEngineGUI
{
	public partial class YandexAuthForm : Form
	{
		private readonly Regex regToken = new("access_token=(?<token>[^&]+)", RegexOptions.Compiled);
		
		public string ClientId { get; set; }
		public string Token { get; set; }

		public YandexAuthForm()
		{
			InitializeComponent();
			webView21.NavigationCompleted += WebView21_NavigationCompleted;
		}

		private void WebView21_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
		{
			string url = ((WebView2)sender!).Source.ToString();
			if (url.StartsWith("https://oauth.yandex.ru/verification_code"))
			{
				var m = regToken.Match(url);
				if (m.Success)
				{
					Token = m.Groups["token"].Value;
				}

				this.Close();
			}
		}

		private void YandexAuthForm_Load(object sender, EventArgs e)
		{
			webView21.Source = new Uri($"https://oauth.yandex.ru/authorize?response_type=token&client_id={ClientId}");
		}
	}
}
