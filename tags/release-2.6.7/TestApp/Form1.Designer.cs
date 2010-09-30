namespace TestApp
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.mnuHelp = new System.Windows.Forms.MenuItem();
            this.mnuCheckUpdates = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.yourMenuItem = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cvxcvxcvxcvxcvToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lblVersion = new System.Windows.Forms.Label();
            this.btnRecheckNow = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.automaticUpdater = new wyDay.Controls.AutomaticUpdater();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.automaticUpdater)).BeginInit();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuHelp});
            // 
            // mnuHelp
            // 
            this.mnuHelp.Index = 0;
            this.mnuHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuCheckUpdates,
            this.menuItem1,
            this.yourMenuItem,
            this.menuItem3});
            this.mnuHelp.Text = "Help";
            // 
            // mnuCheckUpdates
            // 
            this.mnuCheckUpdates.Index = 0;
            this.mnuCheckUpdates.Text = " ";
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 1;
            this.menuItem1.Text = "-";
            // 
            // yourMenuItem
            // 
            this.yourMenuItem.Index = 2;
            this.yourMenuItem.Text = "Online &Help";
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 3;
            this.menuItem3.Text = "&About";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cvxcvxcvxcvxcvToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(155, 26);
            // 
            // cvxcvxcvxcvxcvToolStripMenuItem
            // 
            this.cvxcvxcvxcvxcvToolStripMenuItem.Name = "cvxcvxcvxcvxcvToolStripMenuItem";
            this.cvxcvxcvxcvxcvToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.cvxcvxcvxcvxcvToolStripMenuItem.Text = "cvxcvxcvxcvxcv";
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(12, 15);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(60, 13);
            this.lblVersion.TabIndex = 4;
            this.lblVersion.Text = "Version 1.1";
            // 
            // btnRecheckNow
            // 
            this.btnRecheckNow.Location = new System.Drawing.Point(275, 70);
            this.btnRecheckNow.Name = "btnRecheckNow";
            this.btnRecheckNow.Size = new System.Drawing.Size(108, 24);
            this.btnRecheckNow.TabIndex = 5;
            this.btnRecheckNow.Text = "Recheck now";
            this.btnRecheckNow.UseVisualStyleBackColor = true;
            this.btnRecheckNow.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 48);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(244, 98);
            this.textBox1.TabIndex = 6;
            // 
            // automaticUpdater
            // 
            this.automaticUpdater.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.automaticUpdater.Arguments = "-applesauce=\"C:\\Windows\\Program Files\\File.exe\" -chinese";
            this.automaticUpdater.ContainerForm = this;
            this.automaticUpdater.GUID = "8b43fb52-1ebb-4cfa-a387-e83afa5afef3";
            this.automaticUpdater.Location = new System.Drawing.Point(367, 12);
            this.automaticUpdater.MenuItem = this.mnuCheckUpdates;
            this.automaticUpdater.Name = "automaticUpdater";
            this.automaticUpdater.Size = new System.Drawing.Size(16, 16);
            this.automaticUpdater.TabIndex = 3;
            this.automaticUpdater.UpdateType = wyDay.Controls.UpdateType.CheckAndDownload;
            this.automaticUpdater.wyUpdateCommandline = resources.GetString("automaticUpdater.wyUpdateCommandline");
            this.automaticUpdater.CheckingFailed += new wyDay.Controls.FailHandler(this.automaticUpdater_CheckingFailed);
            this.automaticUpdater.ClosingAborted += new System.EventHandler(this.automaticUpdater_ClosingAborted);
            this.automaticUpdater.UpdateAvailable += new System.EventHandler(this.automaticUpdater_UpdateAvailable);
            this.automaticUpdater.UpdateFailed += new wyDay.Controls.FailHandler(this.automaticUpdater_UpdateFailed);
            this.automaticUpdater.UpdateSuccessful += new wyDay.Controls.SuccessHandler(this.automaticUpdater_UpdateSuccessful);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(395, 163);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.automaticUpdater);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.btnRecheckNow);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.mainMenu1;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "YourApp";
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.automaticUpdater)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private wyDay.Controls.AutomaticUpdater automaticUpdater;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem mnuHelp;
        private System.Windows.Forms.MenuItem mnuCheckUpdates;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem yourMenuItem;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem cvxcvxcvxcvxcvToolStripMenuItem;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Button btnRecheckNow;
        private System.Windows.Forms.TextBox textBox1;
    }
}

