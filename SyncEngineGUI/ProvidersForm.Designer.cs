namespace SyncEngineGUI
{
	partial class AddProviderForm
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddProviderForm));
			ProvidersForm_Title = new Label();
			AddProviderTabControl = new TabControl();
			SelectProviderPage = new TabPage();
			Next1_btn = new Button();
			ProvidersForm_RadioLocal = new RadioButton();
			ProvidersForm_RadioYandex = new RadioButton();
			label1 = new Label();
			AuthLocalPage = new TabPage();
			SourceFolder_TextBox = new TextBox();
			Browse1_btn = new Button();
			label8 = new Label();
			label6 = new Label();
			Next2_btn = new Button();
			Prev1_btn = new Button();
			SelectRootFolderPage = new TabPage();
			label9 = new Label();
			Browse2_btn = new Button();
			RootFolder_TextBox = new TextBox();
			label7 = new Label();
			Complete_btn = new Button();
			button3 = new Button();
			label2 = new Label();
			folderBrowserDialog1 = new FolderBrowserDialog();
			folderBrowserDialog2 = new FolderBrowserDialog();
			AddProviderTabControl.SuspendLayout();
			SelectProviderPage.SuspendLayout();
			AuthLocalPage.SuspendLayout();
			SelectRootFolderPage.SuspendLayout();
			SuspendLayout();
			// 
			// ProvidersForm_Title
			// 
			ProvidersForm_Title.AutoSize = true;
			ProvidersForm_Title.Font = new Font("Segoe UI Semibold", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
			ProvidersForm_Title.Location = new Point(66, 0);
			ProvidersForm_Title.Name = "ProvidersForm_Title";
			ProvidersForm_Title.Size = new Size(388, 37);
			ProvidersForm_Title.TabIndex = 0;
			ProvidersForm_Title.Text = "Выберите поставщика фалов";
			// 
			// AddProviderTabControl
			// 
			AddProviderTabControl.Controls.Add(SelectProviderPage);
			AddProviderTabControl.Controls.Add(AuthLocalPage);
			AddProviderTabControl.Controls.Add(SelectRootFolderPage);
			AddProviderTabControl.Location = new Point(-8, 0);
			AddProviderTabControl.Margin = new Padding(0);
			AddProviderTabControl.Multiline = true;
			AddProviderTabControl.Name = "AddProviderTabControl";
			AddProviderTabControl.Padding = new Point(0, 0);
			AddProviderTabControl.SelectedIndex = 0;
			AddProviderTabControl.Size = new Size(581, 527);
			AddProviderTabControl.TabIndex = 0;
			// 
			// SelectProviderPage
			// 
			SelectProviderPage.BackColor = SystemColors.Control;
			SelectProviderPage.Controls.Add(Next1_btn);
			SelectProviderPage.Controls.Add(ProvidersForm_RadioLocal);
			SelectProviderPage.Controls.Add(ProvidersForm_RadioYandex);
			SelectProviderPage.Controls.Add(label1);
			SelectProviderPage.Location = new Point(4, 24);
			SelectProviderPage.Margin = new Padding(0);
			SelectProviderPage.Name = "SelectProviderPage";
			SelectProviderPage.Padding = new Padding(35, 3, 35, 30);
			SelectProviderPage.Size = new Size(573, 499);
			SelectProviderPage.TabIndex = 0;
			SelectProviderPage.Text = "SelectProvider";
			// 
			// Next1_btn
			// 
			Next1_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Next1_btn.Location = new Point(455, 429);
			Next1_btn.Name = "Next1_btn";
			Next1_btn.Size = new Size(80, 35);
			Next1_btn.TabIndex = 10;
			Next1_btn.Text = "Далее";
			Next1_btn.UseVisualStyleBackColor = true;
			Next1_btn.Click += Next1_btn_Click;
			// 
			// ProvidersForm_RadioLocal
			// 
			ProvidersForm_RadioLocal.Appearance = Appearance.Button;
			ProvidersForm_RadioLocal.Checked = true;
			ProvidersForm_RadioLocal.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
			ProvidersForm_RadioLocal.Image = (Image)resources.GetObject("ProvidersForm_RadioLocal.Image");
			ProvidersForm_RadioLocal.Location = new Point(35, 93);
			ProvidersForm_RadioLocal.Margin = new Padding(0);
			ProvidersForm_RadioLocal.Name = "ProvidersForm_RadioLocal";
			ProvidersForm_RadioLocal.Size = new Size(164, 165);
			ProvidersForm_RadioLocal.TabIndex = 8;
			ProvidersForm_RadioLocal.TabStop = true;
			ProvidersForm_RadioLocal.Tag = "Local";
			ProvidersForm_RadioLocal.Text = "Локальный диск";
			ProvidersForm_RadioLocal.TextAlign = ContentAlignment.BottomCenter;
			ProvidersForm_RadioLocal.TextImageRelation = TextImageRelation.ImageAboveText;
			ProvidersForm_RadioLocal.UseVisualStyleBackColor = true;
			// 
			// ProvidersForm_RadioYandex
			// 
			ProvidersForm_RadioYandex.Appearance = Appearance.Button;
			ProvidersForm_RadioYandex.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
			ProvidersForm_RadioYandex.Image = (Image)resources.GetObject("ProvidersForm_RadioYandex.Image");
			ProvidersForm_RadioYandex.Location = new Point(205, 93);
			ProvidersForm_RadioYandex.Margin = new Padding(0);
			ProvidersForm_RadioYandex.Name = "ProvidersForm_RadioYandex";
			ProvidersForm_RadioYandex.Size = new Size(164, 165);
			ProvidersForm_RadioYandex.TabIndex = 9;
			ProvidersForm_RadioYandex.TabStop = true;
			ProvidersForm_RadioYandex.Tag = "Yandex";
			ProvidersForm_RadioYandex.Text = "Yandex Disk";
			ProvidersForm_RadioYandex.TextAlign = ContentAlignment.BottomCenter;
			ProvidersForm_RadioYandex.TextImageRelation = TextImageRelation.ImageAboveText;
			ProvidersForm_RadioYandex.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Font = new Font("Segoe UI Semibold", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
			label1.Location = new Point(93, 16);
			label1.Name = "label1";
			label1.Size = new Size(388, 37);
			label1.TabIndex = 7;
			label1.Text = "Выберите поставщика фалов";
			// 
			// AuthLocalPage
			// 
			AuthLocalPage.BackColor = SystemColors.Control;
			AuthLocalPage.Controls.Add(SourceFolder_TextBox);
			AuthLocalPage.Controls.Add(Browse1_btn);
			AuthLocalPage.Controls.Add(label8);
			AuthLocalPage.Controls.Add(label6);
			AuthLocalPage.Controls.Add(Next2_btn);
			AuthLocalPage.Controls.Add(Prev1_btn);
			AuthLocalPage.Location = new Point(4, 24);
			AuthLocalPage.Name = "AuthLocalPage";
			AuthLocalPage.Padding = new Padding(35, 3, 35, 30);
			AuthLocalPage.Size = new Size(573, 499);
			AuthLocalPage.TabIndex = 1;
			AuthLocalPage.Text = "AuthLocal";
			// 
			// SourceFolder_TextBox
			// 
			SourceFolder_TextBox.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
			SourceFolder_TextBox.Location = new Point(70, 214);
			SourceFolder_TextBox.Margin = new Padding(35, 10, 35, 10);
			SourceFolder_TextBox.MaxLength = 256;
			SourceFolder_TextBox.Name = "SourceFolder_TextBox";
			SourceFolder_TextBox.Size = new Size(433, 33);
			SourceFolder_TextBox.TabIndex = 23;
			SourceFolder_TextBox.Text = "E:\\folders\\server";
			SourceFolder_TextBox.TextChanged += SourceFolder_TextBox_TextChanged;
			// 
			// Browse1_btn
			// 
			Browse1_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Browse1_btn.Location = new Point(423, 260);
			Browse1_btn.Name = "Browse1_btn";
			Browse1_btn.Size = new Size(80, 35);
			Browse1_btn.TabIndex = 17;
			Browse1_btn.Text = "Обзор";
			Browse1_btn.UseVisualStyleBackColor = true;
			Browse1_btn.Click += Browse1_btn_Click;
			// 
			// label8
			// 
			label8.AutoSize = true;
			label8.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
			label8.Location = new Point(70, 179);
			label8.Name = "label8";
			label8.Size = new Size(155, 25);
			label8.TabIndex = 15;
			label8.Text = "Папка-источник";
			// 
			// label6
			// 
			label6.AutoSize = true;
			label6.Font = new Font("Segoe UI Semibold", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
			label6.Location = new Point(60, 13);
			label6.Name = "label6";
			label6.Size = new Size(455, 37);
			label6.TabIndex = 13;
			label6.Text = "Выберите папку-источник файлов";
			// 
			// Next2_btn
			// 
			Next2_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Next2_btn.Location = new Point(455, 431);
			Next2_btn.Name = "Next2_btn";
			Next2_btn.Size = new Size(80, 35);
			Next2_btn.TabIndex = 12;
			Next2_btn.Text = "Далее";
			Next2_btn.UseVisualStyleBackColor = true;
			Next2_btn.Click += Next2_btn_Click;
			// 
			// Prev1_btn
			// 
			Prev1_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Prev1_btn.Location = new Point(38, 431);
			Prev1_btn.Name = "Prev1_btn";
			Prev1_btn.Size = new Size(80, 35);
			Prev1_btn.TabIndex = 11;
			Prev1_btn.Text = "Назад";
			Prev1_btn.UseVisualStyleBackColor = true;
			Prev1_btn.Click += Prev1_btn_Click;
			// 
			// SelectRootFolderPage
			// 
			SelectRootFolderPage.BackColor = SystemColors.Control;
			SelectRootFolderPage.Controls.Add(label9);
			SelectRootFolderPage.Controls.Add(Browse2_btn);
			SelectRootFolderPage.Controls.Add(RootFolder_TextBox);
			SelectRootFolderPage.Controls.Add(label7);
			SelectRootFolderPage.Controls.Add(Complete_btn);
			SelectRootFolderPage.Controls.Add(button3);
			SelectRootFolderPage.Location = new Point(4, 24);
			SelectRootFolderPage.Name = "SelectRootFolderPage";
			SelectRootFolderPage.Padding = new Padding(35, 3, 35, 30);
			SelectRootFolderPage.Size = new Size(573, 499);
			SelectRootFolderPage.TabIndex = 3;
			SelectRootFolderPage.Text = "RootFolder";
			// 
			// label9
			// 
			label9.Font = new Font("Segoe UI Semibold", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
			label9.Location = new Point(114, 13);
			label9.Name = "label9";
			label9.Size = new Size(344, 80);
			label9.TabIndex = 24;
			label9.Text = "Выберите папку - корень\r\n          синхронизации";
			// 
			// Browse2_btn
			// 
			Browse2_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Browse2_btn.Location = new Point(423, 268);
			Browse2_btn.Name = "Browse2_btn";
			Browse2_btn.Size = new Size(80, 35);
			Browse2_btn.TabIndex = 23;
			Browse2_btn.Text = "Обзор";
			Browse2_btn.UseVisualStyleBackColor = true;
			Browse2_btn.Click += Browse2_btn_Click;
			// 
			// RootFolder_TextBox
			// 
			RootFolder_TextBox.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
			RootFolder_TextBox.Location = new Point(70, 222);
			RootFolder_TextBox.Margin = new Padding(35, 10, 35, 10);
			RootFolder_TextBox.MaxLength = 256;
			RootFolder_TextBox.Name = "RootFolder_TextBox";
			RootFolder_TextBox.Size = new Size(433, 33);
			RootFolder_TextBox.TabIndex = 22;
			RootFolder_TextBox.Text = "E:\\folders\\client";
			RootFolder_TextBox.TextChanged += RootFolder_TextBox_TextChanged;
			// 
			// label7
			// 
			label7.AutoSize = true;
			label7.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
			label7.Location = new Point(70, 187);
			label7.Name = "label7";
			label7.Size = new Size(338, 25);
			label7.TabIndex = 21;
			label7.Text = "Расположение папки синхронизации";
			// 
			// Complete_btn
			// 
			Complete_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Complete_btn.Location = new Point(455, 439);
			Complete_btn.Name = "Complete_btn";
			Complete_btn.Size = new Size(80, 35);
			Complete_btn.TabIndex = 19;
			Complete_btn.Text = "Готово";
			Complete_btn.UseVisualStyleBackColor = true;
			Complete_btn.Click += Complete_btn_Click;
			// 
			// button3
			// 
			button3.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			button3.Location = new Point(38, 439);
			button3.Name = "button3";
			button3.Size = new Size(80, 35);
			button3.TabIndex = 18;
			button3.Text = "Назад";
			button3.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			label2.Location = new Point(332, 0);
			label2.Name = "label2";
			label2.Size = new Size(241, 40);
			label2.TabIndex = 9;
			// 
			// AddProviderForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(574, 521);
			Controls.Add(label2);
			Controls.Add(AddProviderTabControl);
			Controls.Add(ProvidersForm_Title);
			Name = "AddProviderForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Koneru SyncRoot";
			Load += AddProviderForm_Load;
			AddProviderTabControl.ResumeLayout(false);
			SelectProviderPage.ResumeLayout(false);
			SelectProviderPage.PerformLayout();
			AuthLocalPage.ResumeLayout(false);
			AuthLocalPage.PerformLayout();
			SelectRootFolderPage.ResumeLayout(false);
			SelectRootFolderPage.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label ProvidersForm_Title;
		private RadioButton ProvidersForm_RadioYandexDisk;
		private Label ProvidersForm_LabelLocal;
		private Label ProvidersForm_LabelYandexDisk;
		private TabControl AddProviderTabControl;
		private TabPage SelectProviderPage;
		private RadioButton ProvidersForm_RadioLocal;
		private RadioButton ProvidersForm_RadioYandex;
		private Label label1;
		private Button Next1_btn;
		private Label label2;
		private TabPage AuthLocalPage;
		private Button Prev1_btn;
		private Button Next2_btn;
		private Label label6;
		private Label label8;
		private FolderBrowserDialog folderBrowserDialog1;
		private TextBox RootFolder_TextBox;
		private Button Browse1_btn;
		private TabPage SelectRootFolderPage;
		private Button Browse2_btn;
		private TextBox textBox1;
		private Label label7;
		private Label label9;
		private Button Complete_btn;
		private Button button3;
		private TextBox SourceFolder_TextBox;
		private FolderBrowserDialog folderBrowserDialog2;
	}
}