using SyncEngine;
using SyncEngine.ServerProviders;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Windows.Forms.DataFormats;

namespace SyncEngineGUI
{
	public partial class AddSyncRootForm : Form
	{
		string providerName;
		IServerProvider provider;

		TabControl tabControl;

		public AddSyncRootForm()
		{
			InitializeComponent();
			//this.FormBorderStyle = FormBorderStyle.FixedSingle;
			//this.MaximizeBox = false;
			//this.MinimizeBox = false;
		}

		private void AddProviderForm_Load(object sender, EventArgs e)
		{
			tabControl = Controls.OfType<TabControl>().First();
		}

		#region "SelectProviderPage"
		private void Next1_btn_Click(object sender, EventArgs e)
		{
			var selected = tabControl.SelectedTab.Controls.OfType<RadioButton>().Where(x => x.Checked).First();
			switch (selected.Name)
			{
				case "ProvidersForm_RadioLocal":
					AuthLocal();
					break;

				case "ProvidersForm_RadioYandex":
					AuthYandex();
					break;
			}
		}
		#endregion

		#region "SourceFolderPage"
		private void Browse1_btn_Click(object sender, EventArgs e)
		{
			var sourceFolderTextBox = tabControl.SelectedTab.Controls.OfType<TextBox>().First();
			if (sourceFolderTextBox.Text != string.Empty)
				folderBrowserDialog1.InitialDirectory = sourceFolderTextBox.Text;

			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
				sourceFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
		}

		private void Prev1_btn_Click(object sender, EventArgs e)
		{
			tabControl.SelectTab("SelectProviderPage");
		}

		private void SourceFolder_TextBox_TextChanged(object sender, EventArgs e)
		{
			if (Directory.Exists(((TextBox)sender).Text))
				tabControl.SelectedTab.Controls.OfType<Button>()
					.Where(x => x.Name == "Next2_btn").First().Enabled = true;
			else
				tabControl.SelectedTab.Controls.OfType<Button>()
					.Where(x => x.Name == "Next2_btn").First().Enabled = false;
		}

		private void Next2_btn_Click(object sender, EventArgs e)
		{
			var sourceFolderTextBox = tabControl.SelectedTab.Controls.OfType<TextBox>().First();
			provider = new LocalServerProvider(sourceFolderTextBox.Text);
			SelectRootFolder();
		}
		#endregion

		#region "RootFolderPage"
		private void RootFolder_TextBox_TextChanged(object sender, EventArgs e)
		{
			if (Directory.Exists(((TextBox)sender).Text))
				tabControl.SelectedTab.Controls.OfType<Button>()
					.Where(x => x.Name == "Complete_btn").First().Enabled = true;
			else
				tabControl.SelectedTab.Controls.OfType<Button>()
					.Where(x => x.Name == "Complete_btn").First().Enabled = false;
		}

		private void Browse2_btn_Click(object sender, EventArgs e)
		{
			var rootFolderTextBox = tabControl.SelectedTab.Controls.OfType<TextBox>().First();
			if (rootFolderTextBox.Text != string.Empty)
				folderBrowserDialog1.InitialDirectory = rootFolderTextBox.Text;

			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
				rootFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
		}
		#endregion

		private void AuthLocal()
		{
			providerName = "Local";
			tabControl.SelectTab("AuthLocalPage");
		}

		private void AuthYandex()
		{
			providerName = "Yandex";
			var form = new YandexAuthForm();
			form.ClientId = YandexServerProvider.ClientId;
			//this.Visible = false;
			this.Hide();
			form.ShowDialog();
			form.Close();
			provider = new YandexServerProvider(form.Token);
			//this.Visible = true;
			this.Show();
			SelectRootFolder();
		}

		private void SelectRootFolder()
		{
			tabControl.SelectTab("SelectRootFolderPage");
		}

		private async void Complete_btn_Click(object sender, EventArgs e)
		{
			var rootFolderTextBox = tabControl.SelectedTab.Controls.OfType<TextBox>().First();
			SyncRootInfo rootInfo = await SyncRootRegistrar.Register(rootFolderTextBox.Text, providerName, provider.Token);
			SyncRoot root = new(rootInfo, provider);
			Program.Roots.Add(root);
			await root.Start();
			this.Close();
		}
	}
}