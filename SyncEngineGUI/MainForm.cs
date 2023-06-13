using SyncEngine;
using SyncEngineGUI.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncEngineGUI
{
	public partial class MainForm : Form
	{
		private readonly SyncRoot syncRoot;
		private readonly Panel rootListPanel;

		public MainForm() : base()
		{
			InitializeComponent();

			var wArea = Screen.PrimaryScreen.WorkingArea;
			this.Location = new Point(wArea.Width - this.Width - 45, wArea.Height - this.Height - 20);

			rootListPanel = Controls.OfType<Panel>().Where(x => x.Name == "RootsList_panel").First();
		}

		private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				this.Show();
				this.Activate();
				LoadRootsToForm();
			}
		}

		private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private async void Stop_btn_Click(object sender, EventArgs e)
		{
			Button btn = sender as Button;
			var str = btn.Name[(btn.Name.LastIndexOf('-') + 1)..];
			int i = Convert.ToInt32(str);
			var root = Program.Roots[i];
			await root.Stop();
		}

		private void AddRoot_btn_Click(object sender, EventArgs e)
		{
			var form = new AddSyncRootForm();
			form.ShowDialog();
			LoadRootsToForm();
		}

		private async void Unregister_btn_Click(object sender, EventArgs e)
		{
			Button btn = sender as Button;
			var str = btn.Name[(btn.Name.LastIndexOf('-') + 1)..];
			int i = Convert.ToInt32(str);
			var root = Program.Roots[i];
			Program.Roots.RemoveAt(i);
			LoadRootsToForm();
			await root.Stop();
			SyncRootRegistrar.Unregister(root.Info.Id);
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			this.Hide();
		}

		private void LoadRootsToForm()
		{
			List<Panel> panels = new();
			for (int i = 0; i < Program.Roots.Count; i++)
			{
				var root = Program.Roots[i];

				Panel panel = new()
				{
					Name = $"RootPanel-{i}",
					BorderStyle = BorderStyle.FixedSingle,
					Location = new Point(0, i * 40),
					Width = 324,
					Height = 40,
				};

				Label label = new()
				{
					Name = $"RootName-{i}",
					Font = new Font("Segoe UI", 12),
					Text = root.Info.Name,
					Location = new Point(3, 8),
					Size = new Size(231, 21)
				};
				panel.Controls.Add(label);

				Button stopBtn = new()
				{
					Name = $"Stop_btn-{i}",
					FlatStyle = FlatStyle.Flat,
					Image = new Bitmap(Resources.x_icon_32x32),
					Location = new Point(240, 1),
					Size = new Size(35, 35)
				};
				stopBtn.Click += Stop_btn_Click!;
				panel.Controls.Add(stopBtn);

				Button unregisterBtn = new()
				{
					Name = $"Unregister_btn-{i}",
					FlatStyle = FlatStyle.Flat,
					Image = new Bitmap(Resources.x_icon_32x32),
					Location = new Point(281, 1),
					Size = new Size(35, 35)
				};
				unregisterBtn.Click += Unregister_btn_Click!;
				panel.Controls.Add(unregisterBtn);

				panels.Add(panel);
			}

			rootListPanel.Controls.Clear();
			rootListPanel.Controls.AddRange(panels.ToArray());
		}
	}
}
