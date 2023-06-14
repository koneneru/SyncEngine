namespace SyncEngineGUI
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			TrayIcon = new NotifyIcon(components);
			contextMenuStrip1 = new ContextMenuStrip(components);
			CloseToolStripMenuItem = new ToolStripMenuItem();
			AddRoot_btn = new Button();
			RootPanel = new Panel();
			Stop_btn = new Button();
			Unregister_btn = new Button();
			RootName_label = new Label();
			RootsList_panel = new Panel();
			button1 = new Button();
			contextMenuStrip1.SuspendLayout();
			RootPanel.SuspendLayout();
			RootsList_panel.SuspendLayout();
			SuspendLayout();
			// 
			// TrayIcon
			// 
			TrayIcon.BalloonTipText = "KoneruSyncRoot";
			TrayIcon.BalloonTipTitle = "KoneruSyncRoot";
			TrayIcon.ContextMenuStrip = contextMenuStrip1;
			TrayIcon.Icon = (Icon)resources.GetObject("TrayIcon.Icon");
			TrayIcon.Text = "SyncEngine";
			TrayIcon.Visible = true;
			TrayIcon.MouseClick += TrayIcon_MouseClick;
			// 
			// contextMenuStrip1
			// 
			contextMenuStrip1.Items.AddRange(new ToolStripItem[] { CloseToolStripMenuItem });
			contextMenuStrip1.Name = "contextMenuStrip1";
			contextMenuStrip1.Size = new Size(110, 26);
			// 
			// CloseToolStripMenuItem
			// 
			CloseToolStripMenuItem.Name = "CloseToolStripMenuItem";
			CloseToolStripMenuItem.Size = new Size(109, 22);
			CloseToolStripMenuItem.Text = "Выйти";
			CloseToolStripMenuItem.Click += CloseToolStripMenuItem_Click;
			// 
			// AddRoot_btn
			// 
			AddRoot_btn.FlatAppearance.BorderSize = 0;
			AddRoot_btn.FlatStyle = FlatStyle.Flat;
			AddRoot_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			AddRoot_btn.Image = (Image)resources.GetObject("AddRoot_btn.Image");
			AddRoot_btn.Location = new Point(18, 23);
			AddRoot_btn.Margin = new Padding(3, 3, 15, 10);
			AddRoot_btn.Name = "AddRoot_btn";
			AddRoot_btn.Size = new Size(35, 35);
			AddRoot_btn.TabIndex = 26;
			AddRoot_btn.UseVisualStyleBackColor = true;
			AddRoot_btn.Click += AddRoot_btn_Click;
			// 
			// RootPanel
			// 
			RootPanel.BorderStyle = BorderStyle.FixedSingle;
			RootPanel.Controls.Add(Stop_btn);
			RootPanel.Controls.Add(Unregister_btn);
			RootPanel.Controls.Add(RootName_label);
			RootPanel.Location = new Point(0, 0);
			RootPanel.Name = "RootPanel";
			RootPanel.Size = new Size(321, 40);
			RootPanel.TabIndex = 28;
			RootPanel.Visible = false;
			// 
			// Stop_btn
			// 
			Stop_btn.FlatAppearance.BorderSize = 0;
			Stop_btn.FlatStyle = FlatStyle.Flat;
			Stop_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Stop_btn.Image = (Image)resources.GetObject("Stop_btn.Image");
			Stop_btn.Location = new Point(240, 1);
			Stop_btn.Name = "Stop_btn";
			Stop_btn.Size = new Size(35, 35);
			Stop_btn.TabIndex = 30;
			Stop_btn.UseVisualStyleBackColor = true;
			Stop_btn.Click += Pause_btn_Click;
			// 
			// Unregister_btn
			// 
			Unregister_btn.FlatAppearance.BorderSize = 0;
			Unregister_btn.FlatStyle = FlatStyle.Flat;
			Unregister_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Unregister_btn.Image = (Image)resources.GetObject("Unregister_btn.Image");
			Unregister_btn.Location = new Point(281, 1);
			Unregister_btn.Name = "Unregister_btn";
			Unregister_btn.Size = new Size(35, 35);
			Unregister_btn.TabIndex = 29;
			Unregister_btn.UseVisualStyleBackColor = true;
			Unregister_btn.Click += Unregister_btn_Click;
			// 
			// RootName_label
			// 
			RootName_label.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			RootName_label.Location = new Point(3, 8);
			RootName_label.Name = "RootName_label";
			RootName_label.Size = new Size(231, 21);
			RootName_label.TabIndex = 0;
			RootName_label.Text = "SE-Yandex --- Token";
			// 
			// RootsList_panel
			// 
			RootsList_panel.Controls.Add(button1);
			RootsList_panel.Controls.Add(RootPanel);
			RootsList_panel.Location = new Point(18, 71);
			RootsList_panel.Name = "RootsList_panel";
			RootsList_panel.Size = new Size(342, 474);
			RootsList_panel.TabIndex = 29;
			// 
			// button1
			// 
			button1.FlatAppearance.BorderSize = 0;
			button1.FlatStyle = FlatStyle.Flat;
			button1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			button1.Image = (Image)resources.GetObject("button1.Image");
			button1.Location = new Point(241, 43);
			button1.Name = "button1";
			button1.Size = new Size(35, 35);
			button1.TabIndex = 31;
			button1.UseVisualStyleBackColor = true;
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(360, 640);
			ControlBox = false;
			Controls.Add(RootsList_panel);
			Controls.Add(AddRoot_btn);
			FormBorderStyle = FormBorderStyle.None;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "MainForm";
			Padding = new Padding(15, 20, 15, 10);
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Text = "Koneru SyncRoot";
			Deactivate += MainForm_Deactivate;
			contextMenuStrip1.ResumeLayout(false);
			RootPanel.ResumeLayout(false);
			RootsList_panel.ResumeLayout(false);
			ResumeLayout(false);
		}

		#endregion
		private Button Unregister_btn;
		private NotifyIcon TrayIcon;
		private Button AddRoot_btn;
		private Panel RootPanel;
		private Label RootName_label;
		private ContextMenuStrip contextMenuStrip1;
		private ToolStripMenuItem CloseToolStripMenuItem;
		private Button DeleteRoot_btn;
		private Panel RootsList_panel;
		private Button Stop_btn;
		private Button button1;
	}
}