using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using wyUpdate.Common;

/*
namespace wyDay.Controls
{
    /// <summary>
    /// Backend for the AutomaticUpdater control.
    /// </summary>
    public class AutomaticUpdaterBackend
    {
        AutoUpdaterInfo AutoUpdaterInfo;

        UpdateHelper updateHelper = new UpdateHelper();
        string m_wyUpdateLocation = "wyUpdate.exe";

        string m_wyUpdateCommandline;

        UpdateType internalUpdateType = UpdateType.Automatic;
        UpdateType m_UpdateType = UpdateType.Automatic;


        // changes
        string version, changes;
        bool changesAreRTF;
        List<RichTextBoxLink> changesLinks;
        bool ShowButtonUpdateNow;

        string currentActionText;



        /// <summary>
        /// Event is raised before the update checking begins.
        /// </summary>
        public event BeforeHandler BeforeChecking;

        /// <summary>
        /// Event is raised before the downloading of the update begins.
        /// </summary>
        public event BeforeHandler BeforeDownloading;

        /// <summary>
        /// Event is raised when checking or updating is cancelled.
        /// </summary>
        public event EventHandler Cancelled;

        /// <summary>
        /// Event is raised when the checking for updates fails.
        /// </summary>
        public event FailHandler CheckingFailed;

        /// <summary>
        /// Event is raised when an update can't be installed and the closing is aborted.
        /// </summary>
        public event EventHandler ClosingAborted;

        /// <summary>
        /// Event is raised when the update fails to download or extract.
        /// </summary>
        public event FailHandler DownloadingOrExtractingFailed;

        /// <summary>
        /// Event is raised when the current update step progress changes.
        /// </summary>
        public event UpdateProgressChanged ProgressChanged;

        /// <summary>
        /// Event is raised when the update is ready to be installed.
        /// </summary>
        public event EventHandler ReadyToBeInstalled;

        /// <summary>
        /// Event is raised when a new update is found.
        /// </summary>
        public event EventHandler UpdateAvailable;

        /// <summary>
        /// Event is raised when an update fails to install.
        /// </summary>
        public event FailHandler UpdateFailed;

        /// <summary>
        /// Event is raised when an update installs successfully.
        /// </summary>
        public event SuccessHandler UpdateSuccessful;

        /// <summary>
        /// Event is raised when the latest version is already installed.
        /// </summary>
        public event SuccessHandler UpToDate;


        /// <summary>
        /// Gets or sets the arguments to pass to your app when it's being restarted after an update.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Gets the changes for the new update.
        /// </summary>
        public string Changes
        {
            get
            {
                if (!changesAreRTF)
                    return changes;

                // convert the RTF text to plaintext
                using (RichTextBox r = new RichTextBox { Rtf = changes })
                {
                    return r.Text;
                }
            }
        }

        /// <summary>
        /// Gets if this AutomaticUpdater has hidden this form and preparing to install an update.
        /// </summary>
        public bool ClosingForInstall { get; private set; }

        string m_GUID;

        /// <summary>
        /// Gets the GUID (Globally Unique ID) of the automatic updater. It is recommended you set this value (especially if there is more than one exe for your product).
        /// </summary>
        /// <exception cref="System.Exception">Thrown when trying to set the GUID at runtime.</exception>
        public string GUID
        {
            get { return m_GUID; }
            set
            {
                // disallow setting after AutoUpdaterInfo is not null
                if (AutoUpdaterInfo != null)
                    throw new Exception("You must set the GUID at Design time.");

                if (value.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                {
                    // there are bad filename characters
                    throw new Exception("The GUID cannot contain invalid filename characters.");
                }

                m_GUID = value;
            }
        }

        UpdateStepOn m_UpdateStepOn;

        /// <summary>
        /// Gets the update step the AutomaticUpdater is currently on.
        /// </summary>
        public UpdateStepOn UpdateStepOn
        {
            get
            {
                return m_UpdateStepOn;
            }
            private set
            {
                m_UpdateStepOn = value;

                // set the AutoUpdaterInfo property
                if (value != UpdateStepOn.Checking
                    && value != UpdateStepOn.DownloadingUpdate
                    && value != UpdateStepOn.ExtractingUpdate)
                {
                    if (value == UpdateStepOn.Nothing)
                        AutoUpdaterInfo.ClearSuccessError();

                    AutoUpdaterInfo.UpdateStepOn = value;
                    AutoUpdaterInfo.Save();
                }
            }
        }


        /// <summary>
        /// Construct the 
        /// </summary>
        public AutomaticUpdaterBackend()
        {
            //TODO: any initializations
            updateHelper.ProgressChanged += updateHelper_ProgressChanged;
            updateHelper.PipeServerDisconnected += updateHelper_PipeServerDisconnected;
            updateHelper.UpdateStepMismatch += updateHelper_UpdateStepMismatch;
        }

        public void EndInit()
        {
            // read settings file for last check time
            AutoUpdaterInfo = new AutoUpdaterInfo(m_GUID, null);

            // see if update is pending, if so force install
            if (AutoUpdaterInfo.UpdateStepOn == UpdateStepOn.UpdateReadyToInstall)
            {
                // then KillSelf&StartUpdater
                ClosingForInstall = true;

                // start the updater
                InstallPendingUpdate();
            }
        }

        void updateHelper_ProgressChanged(object sender, UpdateHelperData e)
        {
            switch (e.ResponseType)
            {
                case Response.Failed:

                    // and set last successful step
                    UpdateStepFailed(UpdateStepToUpdateStepOn(e.UpdateStep), e.ExtraData[0], e.ExtraData[1]);

                    break;
                case Response.Succeeded:


                    switch (e.UpdateStep)
                    {
                        case UpdateStep.CheckForUpdate:

                            AutoUpdaterInfo.LastCheckedForUpdate = DateTime.Now;

                            // there's an update available
                            if (e.ExtraData.Count != 0)
                            {
                                version = e.ExtraData[0];
                                changes = e.ExtraData[1];
                                changesAreRTF = e.ExtraDataIsRTF[1];
                                changesLinks = e.LinksData;

                                // save the changes to the AutoUpdateInfo file
                                AutoUpdaterInfo.UpdateVersion = version;
                                AutoUpdaterInfo.ChangesInLatestVersion = changes;
                                AutoUpdaterInfo.ChangesIsRTF = changesAreRTF;
                            }
                            else
                            {
                                // Clear saved version details for cases where we're
                                // continuing an update (the version details filled
                                // in from the AutoUpdaterInfo file) however,
                                // wyUpdate reports your app has since been updated.
                                // Thus we need to clear the saved info.
                                version = null;
                                changes = null;
                                changesAreRTF = false;
                                changesLinks = null;

                                AutoUpdaterInfo.ClearSuccessError();
                            }

                            break;
                        case UpdateStep.DownloadUpdate:

                            UpdateStepOn = UpdateStepOn.UpdateDownloaded;

                            break;
                        case UpdateStep.RestartInfo:

                            RestartInfoSent = true;

                            // close this application so it can be updated
                            Application.Exit();

                            break;
                    }

                    StartNextStep(e.UpdateStep);

                    break;
                case Response.Progress:

                    // call the progress changed event
                    if (ProgressChanged != null)
                        ProgressChanged(this, e.Progress);

                    break;
            }
        }

        void StartNextStep(UpdateStep updateStepOn)
        {
            // begin the next step
            switch (updateStepOn)
            {
                case UpdateStep.CheckForUpdate:


                    if (!string.IsNullOrEmpty(version))
                    {
                        // there's an update available


                        if (internalUpdateType == UpdateType.CheckAndDownload
                            || internalUpdateType == UpdateType.Automatic)
                        {
                            UpdateStepOn = UpdateStepOn.UpdateAvailable;

                            // begin downloading the update
                            DownloadUpdate();
                        }
                        else
                        {
                            // show the update ready mark
                            UpdateReady();
                        }
                    }
                    else //no update
                    {
                        // tell the user they're using the latest version
                        AlreadyUpToDate();
                    }

                    break;
                case UpdateStep.DownloadUpdate:

                    // begin extraction
                    if (internalUpdateType == UpdateType.Automatic)
                        ExtractUpdate();
                    else
                        UpdateReadyToExtract();

                    break;
                case UpdateStep.BeginExtraction:

                    // inform the user that the update is ready to be installed
                    UpdateReadyToInstall();

                    break;
            }
        }



        /// <summary>
        /// Check for updates forcefully.
        /// </summary>
        /// <param name="recheck">Recheck with the servers regardless of cached updates, etc.</param>
        /// <returns>Returns true if checking has begun, false otherwise.</returns>
        public bool ForceCheckForUpdate(bool recheck)
        {
            // if not already checking for updates then begin checking.
            if (recheck || UpdateStepOn == UpdateStepOn.Nothing)
            {
                forceCheck(recheck, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check for updates forcefully.
        /// </summary>
        /// <returns>Returns true if checking has begun, false otherwise.</returns>
        public bool ForceCheckForUpdate()
        {
            return ForceCheckForUpdate(false);
        }

        void InstallPendingUpdate()
        {
            // send the client the arguments that need to run on success and failure
            updateHelper.RestartInfo(Application.ExecutablePath, AutoUpdaterInfo.AutoUpdateID, Arguments);
        }

        void DownloadUpdate()
        {
            BeforeArgs bArgs = new BeforeArgs();

            if (BeforeDownloading != null)
                BeforeDownloading(this, bArgs);

            if (bArgs.Cancel)
            {
                // close wyUpdate
                updateHelper.Cancel();
                return;
            }

            // if the control is hidden show it now (so the user can cancel the downloading if they want)
            // show the 'working' animation
            SetUpdateStepOn(UpdateStepOn.DownloadingUpdate);
            UpdateProcessing(true);

            CreateMenu(MenuType.CancelDownloading);

            updateHelper.DownloadUpdate();
        }

        void ExtractUpdate()
        {
            SetUpdateStepOn(UpdateStepOn.ExtractingUpdate);

            CreateMenu(MenuType.CancelExtracting);

            // extract the update
            updateHelper.BeginExtraction();
        }

        void updateHelper_UpdateStepMismatch(object sender, Response respType, UpdateStep previousStep)
        {
            // we can't install right now
            if (previousStep == UpdateStep.RestartInfo)
            {
                // we need to show the form (it was hidden in ISupport() )
                if (ClosingForInstall)
                {
                    ownerForm.ShowInTaskbar = true;
                    ownerForm.WindowState = FormWindowState.Normal;
                }

                if (ClosingAborted != null)
                    ClosingAborted(this, EventArgs.Empty);
            }

            if (respType == Response.Progress)
            {
                switch (updateHelper.UpdateStep)
                {
                    case UpdateStep.CheckForUpdate:
                        SetUpdateStepOn(UpdateStepOn.Checking);
                        break;
                    case UpdateStep.DownloadUpdate:
                        SetUpdateStepOn(UpdateStepOn.DownloadingUpdate);
                        break;
                    case UpdateStep.BeginExtraction:
                        SetUpdateStepOn(UpdateStepOn.ExtractingUpdate);
                        break;
                }
            }
        }

        void updateHelper_PipeServerDisconnected(object sender, EventArgs e)
        {
            // the client should only ever exit after success or failure
            // otherwise it is a premature exit (and needs to be treated as an error)
            if (UpdateStepOn == UpdateStepOn.Checking
                || UpdateStepOn == UpdateStepOn.DownloadingUpdate
                || UpdateStepOn == UpdateStepOn.ExtractingUpdate)
            {
                UpdateStepFailed(UpdateStepOn, translation.PrematureExitTitle, translation.PrematureExitMessage);
            }
        }

        /// <summary>
        /// Cancel the checking, downloading, or extracting currently in progress.
        /// </summary>
        public void Cancel()
        {
            updateHelper.Cancel();

            SetLastSuccessfulStep();

            if (Cancelled != null)
                Cancelled(this, EventArgs.Empty);
        }

        void SetLastSuccessfulStep()
        {
            UpdateStepOn = UpdateStepOn == UpdateStepOn.Checking ? UpdateStepOn.Nothing : UpdateStepOn.UpdateAvailable;
        }


        void UpdateStepFailed(UpdateStepOn us, string errorTitle, string errorMessage)
        {
            SetLastSuccessfulStep();

            FailArgs failArgs = new FailArgs { ErrorTitle = errorTitle, ErrorMessage = errorMessage };

            switch (us)
            {
                case UpdateStepOn.Checking:
                    
                    if (CheckingFailed != null)
                        CheckingFailed(this, failArgs);

                    break;
                case UpdateStepOn.DownloadingUpdate:
                    
                    if (DownloadingOrExtractingFailed != null)
                        DownloadingOrExtractingFailed(this, failArgs);

                    break;
                case UpdateStepOn.ExtractingUpdate:

                    if (DownloadingOrExtractingFailed != null)
                        DownloadingOrExtractingFailed(this, failArgs);

                    break;
            }
        }

        static UpdateStepOn UpdateStepToUpdateStepOn(UpdateStep us)
        {
            switch (us)
            {
                case UpdateStep.BeginExtraction:
                    return UpdateStepOn.ExtractingUpdate;
                case UpdateStep.CheckForUpdate:
                    return UpdateStepOn.Checking;
                case UpdateStep.DownloadUpdate:
                    return UpdateStepOn.DownloadingUpdate;
                default:
                    throw new Exception("UpdateStep not supported");
            }
        }
    }
}
*/