using SyncEngine;
using SyncEngine.ServerProviders;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Windows.Forms.DataFormats;

namespace SyncEngineGUI
{
	public partial class AddProviderForm : Form
	{
		IServerProvider provider;

		TabControl tabControl;

		public AddProviderForm()
		{
			InitializeComponent();
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
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
			tabControl.SelectTab("AuthLocalPage");
		}

		private void AuthYandex()
		{
			var form = new YandexAuthForm();
			form.ClientId = YandexServerProvider.ClientId;
			this.Visible = false;
			form.ShowDialog();
			form.Close();
			provider = new YandexServerProvider(form.Token);
			this.Visible = true;
			SelectRootFolder();
		}

		private void SelectRootFolder()
		{
			tabControl.SelectTab("SelectRootFolderPage");
		}

		private void Complete_btn_Click(object sender, EventArgs e)
		{
			var rootFolderTextBox = tabControl.SelectedTab.Controls.OfType<TextBox>().First();
			SyncRoot syncRoot = new(rootFolderTextBox.Text, provider);
			Program.Context.MainForm = new MainForm(syncRoot);
			this.Close();
			Program.Context.MainForm.Show();
		}
	}
}