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
			Stop_btn = new Button();
			Unregister_btn = new Button();
			SuspendLayout();
			// 
			// Stop_btn
			// 
			Stop_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Stop_btn.Location = new Point(13, 18);
			Stop_btn.Margin = new Padding(3, 3, 15, 10);
			Stop_btn.Name = "Stop_btn";
			Stop_btn.Size = new Size(95, 35);
			Stop_btn.TabIndex = 24;
			Stop_btn.Text = "Stop";
			Stop_btn.UseVisualStyleBackColor = true;
			Stop_btn.Click += Stop_btn_Click;
			// 
			// Unregister_btn
			// 
			Unregister_btn.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			Unregister_btn.Location = new Point(131, 18);
			Unregister_btn.Name = "Unregister_btn";
			Unregister_btn.Size = new Size(95, 35);
			Unregister_btn.TabIndex = 25;
			Unregister_btn.Text = "Unregister";
			Unregister_btn.UseVisualStyleBackColor = true;
			Unregister_btn.Click += Unregister_btn_Click;
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(239, 73);
			Controls.Add(Unregister_btn);
			Controls.Add(Stop_btn);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "MainForm";
			Padding = new Padding(10, 15, 10, 10);
			Text = "Koneru SyncRoot";
			Load += MainForm_Load;
			ResumeLayout(false);
		}

		#endregion

		private Button Stop_btn;
		private Button Unregister_btn;
	}
}