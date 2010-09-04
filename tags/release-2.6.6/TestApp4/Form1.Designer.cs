namespace TestApp4
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
            wyDay.Controls.AUTranslation auTranslation1 = new wyDay.Controls.AUTranslation();
            this.automaticUpdater1 = new wyDay.Controls.AutomaticUpdater();
            ((System.ComponentModel.ISupportInitialize)(this.automaticUpdater1)).BeginInit();
            this.SuspendLayout();
            // 
            // automaticUpdater1
            // 
            this.automaticUpdater1.ContainerForm = this;
            this.automaticUpdater1.Location = new System.Drawing.Point(369, 12);
            this.automaticUpdater1.Name = "automaticUpdater1";
            this.automaticUpdater1.Size = new System.Drawing.Size(16, 16);
            this.automaticUpdater1.TabIndex = 0;
            auTranslation1.AlreadyUpToDate = "You already have the latest version";
            auTranslation1.CancelCheckingMenu = "&Cancel update checking";
            auTranslation1.CancelUpdatingMenu = "&Cancel updating";
            auTranslation1.ChangesInVersion = "Changes in version %version%";
            auTranslation1.CheckForUpdatesMenu = "&Check for updates";
            auTranslation1.Checking = "Checking for updates";
            auTranslation1.CloseButton = "Close";
            auTranslation1.Downloading = "Downloading update";
            auTranslation1.DownloadUpdateMenu = "&Download and Update now";
            auTranslation1.ErrorTitle = "Error";
            auTranslation1.Extracting = "Extracting update";
            auTranslation1.FailedToCheck = "Failed to check for updates.";
            auTranslation1.FailedToDownload = "Failed to download the update.";
            auTranslation1.FailedToExtract = "Failed to extract the update.";
            auTranslation1.HideMenu = "Hide";
            auTranslation1.InstallOnNextStart = "Update will be installed on next start.";
            auTranslation1.InstallUpdateMenu = "&Install update now";
            auTranslation1.PrematureExitMessage = "wyUpdate ended before the current update step could be completed.";
            auTranslation1.PrematureExitTitle = "wyUpdate exited prematurely";
            auTranslation1.StopChecking = "Stop checking for updates for now";
            auTranslation1.StopDownloading = "Stop downloading update for now";
            auTranslation1.StopExtracting = "Stop extracting update for now";
            auTranslation1.SuccessfullyUpdated = "Successfully updated to %version%";
            auTranslation1.TryAgainLater = "Try again later";
            auTranslation1.TryAgainNow = "Try again now";
            auTranslation1.UpdateAvailable = "Update is ready to be installed.";
            auTranslation1.UpdateFailed = "Update failed to install.";
            auTranslation1.UpdateNowButton = "Update now";
            auTranslation1.ViewChangesMenu = "View changes in version %version%";
            auTranslation1.ViewError = "View error details";
            this.automaticUpdater1.Translation = auTranslation1;
            this.automaticUpdater1.wyUpdateCommandline = null;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(416, 225);
            this.Controls.Add(this.automaticUpdater1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.automaticUpdater1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private wyDay.Controls.AutomaticUpdater automaticUpdater1;


    }
}

