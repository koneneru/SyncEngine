using SyncEngine;
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
		private SyncRoot syncRoot;

		public MainForm(SyncRoot root) : base()
		{
			syncRoot = root;
			InitializeComponent();
		}

		private async void MainForm_Load(object sender, EventArgs e)
		{
			await syncRoot.Start();
		}

		private async void Stop_btn_Click(object sender, EventArgs e)
		{
			await syncRoot.Stop();
		}

		private async void Unregister_btn_Click(object sender, EventArgs e)
		{
			await syncRoot.Unregister();
		}
	}
}
