using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using wyUpdate.Common;

namespace wyDay.Controls
{

#if NET_2_0
    /// <summary>
    /// Represents the AutomaticUpdater control.
    /// </summary>
    [Designer(typeof(AutomaticUpdaterDesigner))]
#else
    /// <summary>
    /// Represents the AutomaticUpdater control.
    /// </summary>
    [Designer("wyDay.Controls.AutomaticUpdaterDesigner, AutomaticUpdater.Design")]
#endif
    [ToolboxBitmapAttribute(typeof(AutomaticUpdater), "update-notify.png")]
    public class AutomaticUpdater : ContainerControl, ISupportInitialize
    {
        AutoUpdaterInfo AutoUpdaterInfo;

        Form ownerForm;

        readonly AnimationControl ani = new AnimationControl();

        Rectangle textRect;

        UpdateHelper updateHelper;
        string m_wyUpdateLocation = "wyUpdate.exe";

        string m_wyUpdateCommandline;

        UpdateType internalUpdateType = UpdateType.Automatic;
        UpdateType m_UpdateType = UpdateType.Automatic;


        readonly Timer tmrCollapse = new Timer { Interval = 3000 };
        readonly Timer tmrAniExpandCollapse = new Timer { Interval = 30 };

        int m_WaitBeforeCheckSecs = 10;
        readonly Timer tmrWaitBeforeCheck = new Timer { Interval = 10000 };

        int m_DaysBetweenChecks = 12;



        ContextMenu contextMenu;
        MenuType CurrMenuType = MenuType.Nothing;
        bool isMenuVisible;

        // changes
        string version, changes;
        bool changesAreRTF;
        List<RichTextBoxLink> changesLinks;
        bool ShowButtonUpdateNow;

        string currentActionText;


        // error
        string errorTitle, errorMessage;

        bool RestartInfoSent;

        // menu items
        ToolStripItem toolStripItem;
        MenuItem menuItem;

        static readonly Bitmap BmpInfo = new Bitmap(typeof (AutomaticUpdater), "info.png");
        static readonly Bitmap BmpWorking = new Bitmap(typeof (AutomaticUpdater), "update-working.png");
        static readonly Bitmap BmpNotify = new Bitmap(typeof (AutomaticUpdater), "update-notify.png");
        static readonly Bitmap BmpSuccess = new Bitmap(typeof (AutomaticUpdater), "tick.png");
        static readonly Bitmap BmpFailed = new Bitmap(typeof (AutomaticUpdater), "cross.png");

        SolidBrush backBrush;

        #region Events

        /// <summary>
        /// Event is raised before the update checking begins.
        /// </summary>
        [Description("Event is raised before the update checking begins."),
        Category("Updater")]
        public event BeforeHandler BeforeChecking;

        /// <summary>
        /// Event is raised before the downloading of the update begins.
        /// </summary>
        [Description("Event is raised before the downloading of the update begins."),
        Category("Updater")]
        public event BeforeHandler BeforeDownloading;
        
        /// <summary>
        /// Event is raised when checking or updating is cancelled.
        /// </summary>
        [Description("Event is raised when checking or updating is cancelled."),
        Category("Updater")]
        public event EventHandler Cancelled;

        /// <summary>
        /// Event is raised when the checking for updates fails.
        /// </summary>
        [Description("Event is raised when the checking for updates fails."),
        Category("Updater")]
        public event FailHandler CheckingFailed;

        /// <summary>
        /// Event is raised when an update can't be installed and the closing is aborted.
        /// </summary>
        [Description("Event is raised when an update can't be installed and the closing is aborted."),
        Category("Updater")]
        public event EventHandler ClosingAborted;

        /// <summary>
        /// Event is raised when the update fails to download or extract.
        /// </summary>
        [Description("Event is raised when the update fails to download or extract."),
        Category("Updater")]
        public event FailHandler DownloadingOrExtractingFailed;

        /// <summary>
        /// Event is raised when the current update step progress changes.
        /// </summary>
        [Description("Event is raised when the current update step progress changes."),
        Category("Updater")]
        public event UpdateProgressChanged ProgressChanged;

        /// <summary>
        /// Event is raised when the update is ready to be installed.
        /// </summary>
        [Description("Event is raised when the update is ready to be installed."),
        Category("Updater")]
        public event EventHandler ReadyToBeInstalled;

        /// <summary>
        /// Event is raised when a new update is found.
        /// </summary>
        [Description("Event is raised when a new update is found."),
        Category("Updater")]
        public event EventHandler UpdateAvailable;

        /// <summary>
        /// Event is raised when an update fails to install.
        /// </summary>
        [Description("Event is raised when an update fails to install."),
        Category("Updater")]
        public event FailHandler UpdateFailed;

        /// <summary>
        /// Event is raised when an update installs successfully.
        /// </summary>
        [Description("Event is raised when an update installs successfully."),
        Category("Updater")]
        public event SuccessHandler UpdateSuccessful;

        /// <summary>
        /// Event is raised when the latest version is already installed.
        /// </summary>
        [Description("Event is raised when the latest version is already installed."),
        Category("Updater")]
        public event SuccessHandler UpToDate;

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler TextChanged;

        #endregion Events

        #region Properties

        /// <summary>
        /// Gets or sets the arguments to pass to your app when it's being restarted after an update.
        /// </summary>
        [Description("The arguments to pass to your app when it's being restarted after an update."),
        DefaultValue(null),
        Category("Updater")]
        public string Arguments { get; set; }

        /// <summary>
        /// Gets the changes for the new update.
        /// </summary>
        [Browsable(false)]
        public string Changes
        {
            get
            {
                if (!changesAreRTF)
                    return changes;

                // convert the RTF text to plaintext
                using (RichTextBox r = new RichTextBox {Rtf = changes})
                {
                    return r.Text;
                }
            }
        }

        /// <summary>
        /// Gets if this AutomaticUpdater has hidden this form and preparing to install an update.
        /// </summary>
        [Browsable(false)]
        public bool ClosingForInstall { get; private set; }

        /// <summary>
        /// Gets or sets the number of days to wait before automatically re-checking for updates.
        /// </summary>
        [Description("The number of days to wait before automatically re-checking for updates."),
        DefaultValue(12),
        Category("Updater")]
        public int DaysBetweenChecks
        {
            get { return m_DaysBetweenChecks; }
            set { m_DaysBetweenChecks = value; }
        }

        string m_GUID;

        /// <summary>
        /// Gets the GUID (Globally Unique ID) of the automatic updater. It is recommended you set this value (especially if there is more than one exe for your product).
        /// </summary>
        /// <exception cref="System.Exception">Thrown when trying to set the GUID at runtime.</exception>
        [Description("The GUID (Globally Unique ID) of the automatic updater. It is recommended you set this value (especially if there is more than one exe for your product)."),
        Category("Updater"),
        DefaultValue(null),
        EditorAttribute(typeof(GUIDEditor), typeof(UITypeEditor)),
        EditorBrowsable(EditorBrowsableState.Never)]
        public string GUID
        {
            get { return m_GUID; }
            set
            {
                // disallow setting after AutoUpdaterInfo is not null
                if (AutoUpdaterInfo != null)
                    throw new Exception("You must set the GUID at Design time.");

                if (DesignMode)
                {
                    if (value.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                    {
                        // there are bad filename characters
                        throw new Exception("The GUID cannot contain invalid filename characters.");
                    }
                }

                m_GUID = value;
            }
        }

        bool ShouldSerializeGUID()
        {
            return !string.IsNullOrEmpty(m_GUID);
        }

        /// <summary>
        /// Gets or sets whether the AutomaticUpdater control should stay hidden even when the user should be notified. (Not recommended).
        /// </summary>
        [Description("Keeps the AutomaticUpdater control hidden even when the user should be notified. (Not recommended)"),
        DefaultValue(false),
        Category("Updater")]
        public bool KeepHidden { get; set; }

        /// <summary>
        /// Gets the date the updates were last checked for.
        /// </summary>
        [Browsable(false)]
        public DateTime LastCheckDate
        {
            get { return AutoUpdaterInfo.LastCheckedForUpdate; }
        }

        /// <summary>
        /// Gets and sets the MenuItem that will be used to check for updates.
        /// </summary>
        [Description("The MenuItem that will be used to check for updates."),
        Category("Updater"),
        DefaultValue(null)]
        public MenuItem MenuItem
        {
            get
            {
                return menuItem;
            }
            set
            {
                if (menuItem != null)
                    menuItem.Click -= menuItem_Click;

                menuItem = value;

                if (menuItem != null)
                    menuItem.Click += menuItem_Click;
            }
        }

        /// <summary>
        /// Gets and sets the ToolStripItem that will be used to check for updates.
        /// </summary>
        [Description("The ToolStripItem that will be used to check for updates."),
        Category("Updater"),
        DefaultValue(null)]
        public ToolStripItem ToolStripItem
        {
            get
            {
                return toolStripItem;
            }
            set
            {
                if (toolStripItem != null)
                    toolStripItem.Click -= menuItem_Click;

                toolStripItem = value;

                if (toolStripItem != null)
                    toolStripItem.Click += menuItem_Click;
            }
        }

        AUTranslation translation = new AUTranslation();

        /// <summary>
        /// The translation for the english strings in the AutomaticUpdater control.
        /// </summary>
        /// <exception cref="ArgumentNullException">The translation cannot be null.</exception>
        [Browsable(false)]
        public AUTranslation Translation
        {
            get { return translation; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                translation = value;
            }
        }

        bool ShouldSerializeTranslation() { return false; }

        UpdateStepOn m_UpdateStepOn;

        /// <summary>
        /// Gets the update step the AutomaticUpdater is currently on.
        /// </summary>
        [Browsable(false)]
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
        /// Gets or sets how much this AutomaticUpdater control should do without user interaction.
        /// </summary>
        [Description("How much this AutomaticUpdater control should do without user interaction."),
        DefaultValue(UpdateType.Automatic),
        Category("Updater")]
        public UpdateType UpdateType
        {
            get { return m_UpdateType; }
            set
            {
                m_UpdateType = value;
                internalUpdateType = value;
            }
        }

        /// <summary>
        /// Gets the version of the new update.
        /// </summary>
        [Browsable(false)]
        public string Version
        {
            get
            {
                return version;
            }
        }

        /// <summary>
        /// Gets or sets the seconds to wait after the form is loaded before checking for updates.
        /// </summary>
        [Description("Seconds to wait after the form is loaded before checking for updates."),
        DefaultValue(10),
        Category("Updater")]
        public int WaitBeforeCheckSecs
        {
            get { return m_WaitBeforeCheckSecs; }
            set
            {
                m_WaitBeforeCheckSecs = value;

                tmrWaitBeforeCheck.Interval = m_WaitBeforeCheckSecs * 1000;
            }
        }

        /// <summary>
        /// Gets or sets the arguments to pass to wyUpdate when it is started to check for updates.
        /// </summary>
        [Description("Arguments to pass to wyUpdate when it is started to check for updates."),
        Category("Updater")]
        public string wyUpdateCommandline
        {
            get { return m_wyUpdateCommandline; }
            set
            {
                m_wyUpdateCommandline = value;

                if (updateHelper != null)
                    updateHelper.ExtraArguments = m_wyUpdateCommandline;
            }
        }

        /// <summary>
        /// Gets or sets the relative path to the wyUpdate (e.g. wyUpdate.exe  or  SubDir\\wyUpdate.exe)
        /// </summary>
        [Description("The relative path to the wyUpdate (e.g. wyUpdate.exe  or  SubDir\\wyUpdate.exe)"), 
        DefaultValue("wyUpdate.exe"),
        Category("Updater")]
        public string wyUpdateLocation
        {
            get { return m_wyUpdateLocation; }
            set 
            {
                m_wyUpdateLocation = value;

                if (updateHelper != null)
                    updateHelper.wyUpdateLocation = GetFullWyUpdateLocation();
            }
        }

        string GetFullWyUpdateLocation()
        {
            //if the client path is a relative path, then return the full path relative to this executing program
            if (!Path.IsPathRooted(m_wyUpdateLocation))
                return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), m_wyUpdateLocation);
            
            return m_wyUpdateLocation;
        }

        [Bindable(false), EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;

                RefreshTextRect();

                Invalidate();
            }
        }

        [Browsable(false)]
        public Size Size
        {
            get
            {
                return base.Size;
            }
            set
            {
                base.Size = value;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (DesignMode)
                Size = new Size(16, 16);
            else
                Height = Math.Max(16, Font.Height);

            base.OnSizeChanged(e);
        }

        #endregion

        //Methods and such

        /// <summary>
        /// Represents an AutomaticUpdater control.
        /// </summary>
        public AutomaticUpdater()
        {
            // This turns on double buffering of all custom GDI+ drawing
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.OptimizedDoubleBuffer 
                | ControlStyles.ResizeRedraw 
                | ControlStyles.UserPaint
                | ControlStyles.FixedHeight
                | ControlStyles.FixedWidth, true);

            ani.Size = new Size(16, 16);
            ani.Location = new Point(0, 0);

            ani.Rows = 5;
            ani.Columns = 10;
            Controls.Add(ani);


            ani.MouseEnter += ani_MouseEnter;
            ani.MouseLeave += ani_MouseLeave;
            ani.MouseDown += ani_MouseDown;

            if (DesignMode)
                ani.Visible = false;

            // events for the timers
            tmrCollapse.Tick += tmrCollapse_Tick;
            tmrAniExpandCollapse.Tick += tmrAniExpandCollapse_Tick;
            tmrWaitBeforeCheck.Tick += tmrWaitBeforeCheck_Tick;

            Application.ApplicationExit += Application_ApplicationExit;

            Size = new Size(16, 16);

            backBrush = new SolidBrush(BackColor);
        }

        /// <summary>
        /// Represents an AutomaticUpdater control.
        /// </summary>
        /// <param name="parentControl">The owner form of the AutomaticUpdater.</param>
        public AutomaticUpdater(Form parentControl)
            : this()
        {
            ownerForm = parentControl;

            ownerForm.Load += ownerForm_Load;

            updateHelper = new UpdateHelper(ownerForm)
                               {
                                   wyUpdateLocation = GetFullWyUpdateLocation(),
                                   ExtraArguments = wyUpdateCommandline
                               };

            updateHelper.ProgressChanged += updateHelper_ProgressChanged;
            updateHelper.PipeServerDisconnected += updateHelper_PipeServerDisconnected;
            updateHelper.UpdateStepMismatch += updateHelper_UpdateStepMismatch;
        }



        /// <summary>
        /// The owner form of the AutomaticUpdater.
        /// </summary>
        [Browsable(false)]
        public Form ContainerForm
        {
            get { return ownerForm; }
            set
            {
                if (updateHelper != null)
                {
                    updateHelper.ProgressChanged -= updateHelper_ProgressChanged;
                    updateHelper.PipeServerDisconnected -= updateHelper_PipeServerDisconnected;
                    updateHelper.UpdateStepMismatch -= updateHelper_UpdateStepMismatch;
                }
                    

                if (ownerForm == value)
                    ownerForm.Load -= ownerForm_Load;

                ownerForm = value;

                ownerForm.Load += ownerForm_Load;

                updateHelper = new UpdateHelper(ownerForm)
                                   {
                                       wyUpdateLocation = GetFullWyUpdateLocation(),
                                       ExtraArguments = wyUpdateCommandline
                                   };

                updateHelper.ProgressChanged += updateHelper_ProgressChanged;
                updateHelper.PipeServerDisconnected += updateHelper_PipeServerDisconnected;
                updateHelper.UpdateStepMismatch += updateHelper_UpdateStepMismatch;
            }
        }

        public override ISite Site
        {
            set
            {
                // Runs at design time, ensures designer initializes ContainerControl
                base.Site = value;
                if (value == null) return;
                IDesignerHost service = value.GetService(typeof(IDesignerHost)) as IDesignerHost;
                if (service == null) return;
                IComponent rootComponent = service.RootComponent;
                ContainerForm = rootComponent as Form;
            }
        }
        

        bool insideChildControl;
        bool insideSelf;

        void ani_MouseLeave(object sender, EventArgs e)
        {
            insideChildControl = false;

            if (!insideSelf && isFullExpanded && !isMenuVisible)
                tmrCollapse.Enabled = true;
        }

        void ani_MouseEnter(object sender, EventArgs e)
        {
            insideChildControl = true;

            tmrCollapse.Enabled = false;

            if (!isFullExpanded)
                BeginAniOpen();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            insideSelf = true;

            tmrCollapse.Enabled = false;

            if (!isFullExpanded)
                BeginAniOpen();

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            insideSelf = false;

            if (!insideChildControl && isFullExpanded && !isMenuVisible)
                tmrCollapse.Enabled = true;

            base.OnMouseLeave(e);
        }

        void tmrCollapse_Tick(object sender, EventArgs e)
        {
            // begin collapse animation
            BeginAniClose();

            // disable this timer
            tmrCollapse.Stop();
        }

        bool skipNextMenuShow;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            ani_MouseDown(null, null);
            
            base.OnMouseDown(e);
        }

        void ani_MouseDown(object sender, MouseEventArgs e)
        {
            if (!(insideChildControl || insideSelf))
                //skip next menu show when "reclicking" while the menu is still visible
                skipNextMenuShow = true;

            if (skipNextMenuShow)
                skipNextMenuShow = false;
            else
                ShowContextMenu();
        }

        void ShowContextMenu()
        {
            if (contextMenu != null)
            {
                isMenuVisible = true;

                if ((Anchor & AnchorStyles.Right) == AnchorStyles.Right)
                    contextMenu.Show(this, new Point(Width, Height), LeftRightAlignment.Left);
                else
                    contextMenu.Show(this, new Point(0, Height), LeftRightAlignment.Right);
            }
        }


        #region Animation

        // calculate the expanded width based on the changing text
        int expandedWidth = 16;
        const int collapsedWidth = 16;

        const int maxTimeTicks = 30;
        int totalTimeTicks;
        int startSize, sizeChange;

        bool isFullExpanded;

        void tmrAniExpandCollapse_Tick(object sender, EventArgs e)
        {
            if (startSize - Width + sizeChange == 0)
            {
                isFullExpanded = Width != collapsedWidth;

                //enable the collapse timer
                if(isFullExpanded && !insideChildControl && !insideSelf && !isMenuVisible)
                    tmrCollapse.Enabled = true;

                tmrAniExpandCollapse.Stop();
            }
            else
            {
                totalTimeTicks++;

                int DeltaAnileft;

                if (totalTimeTicks == maxTimeTicks)
                    DeltaAnileft = startSize + sizeChange - Width;
                else
                    DeltaAnileft = (int)(sizeChange * (-Math.Pow(2, (float)(-10 * totalTimeTicks) / maxTimeTicks) + 1) + startSize) - Width;

                Width += DeltaAnileft;

                if ((Anchor & AnchorStyles.Right) == AnchorStyles.Right)
                    Left -= DeltaAnileft;
            }
        }


        void BeginAniClose()
        {
            // totalDist = destX - startX
            sizeChange = collapsedWidth - Width;

            // bail out if no tabs need to be moved
            if (sizeChange == 0)
                return;

            // set the start position
            startSize = Width;

            // begin the scrolling animation
            totalTimeTicks = 0;

            // begin the closing animation
            isFullExpanded = false;
            tmrAniExpandCollapse.Start();
        }

        void BeginAniOpen()
        {
            // totalDist = destX - startX
            sizeChange = expandedWidth - Width;

            // bail out if no tabs need to be moved
            if (sizeChange == 0)
            {
                tmrCollapse.Enabled = true;

                return;
            }
                
            // set the start position
            startSize = Width;

            // begin the scrolling animation
            totalTimeTicks = 0;


            // begin the opening animation
            tmrAniExpandCollapse.Start();
        }

        #endregion Animation

        void menuItem_Click(object sender, EventArgs e)
        {
            switch (UpdateStepOn)
            {
                case UpdateStepOn.Checking:
                case UpdateStepOn.DownloadingUpdate:
                case UpdateStepOn.ExtractingUpdate:

                    Cancel();
                    break;

                case UpdateStepOn.UpdateReadyToInstall:
                case UpdateStepOn.UpdateAvailable:
                case UpdateStepOn.UpdateDownloaded:

                    InstallNow();
                    break;

                default:

                    ForceCheckForUpdate();
                    break;
            }
        }

        void SetMenuText(string text)
        {
            if (menuItem != null)
                menuItem.Text = text;
            else if (toolStripItem != null)
                toolStripItem.Text = text;
        }


        void InstallNow_Click(object sender, EventArgs e)
        {
            InstallNow();
        }

        /// <summary>
        /// Proceed with the download and installation of pending updates.
        /// </summary>
        public void InstallNow()
        {
            // throw an exception when trying to Install when no update is ready

            if (UpdateStepOn == UpdateStepOn.Nothing)
                throw new Exception("There must be an update available before you can install it.");
            
            if (UpdateStepOn == UpdateStepOn.Checking)
                throw new Exception(
                    "The AutomaticUpdater must finish checking for updates before they can be installed.");

            if(UpdateStepOn == UpdateStepOn.DownloadingUpdate)
                throw new Exception("The update must be downloaded before you can install it.");

            if (UpdateStepOn == UpdateStepOn.ExtractingUpdate)
                throw new Exception("The update must finish extracting before you can install it.");

            // set the internal update type to autmatic so the user won't be prompted anymore
            internalUpdateType = UpdateType.Automatic;

            if (UpdateStepOn == UpdateStepOn.UpdateAvailable)
            {
                // begin downloading the update
                DownloadUpdate();
            }
            else // UpdateDownloaded or UpdateReadyToInstall
            {
                // begin installing the update
                InstallPendingUpdate();
            }
        }

        void CancelUpdate_Click(object sender, EventArgs e)
        {
            Cancel();
        }

        void Hide_Click(object sender, EventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// Cancel the checking, downloading, or extracting currently in progress.
        /// </summary>
        public void Cancel()
        {
            // stop animation & hide
            ani.StopAnimation();
            Visible = false;

            updateHelper.Cancel();

            SetLastSuccessfulStep();

            SetMenuText(translation.CheckForUpdatesMenu);

            if (Cancelled != null)
                Cancelled(this, EventArgs.Empty);
        }

        void SetLastSuccessfulStep()
        {
            if (UpdateStepOn == UpdateStepOn.Checking)
                UpdateStepOn = UpdateStepOn.Nothing;
            else
                UpdateStepOn = UpdateStepOn.UpdateAvailable;
        }

        void ViewChanges_Click(object sender, EventArgs e)
        {
            frmChanges changeForm = new frmChanges(version, changes, changesAreRTF, changesLinks, ShowButtonUpdateNow, translation);
            changeForm.ShowDialog(ownerForm);

            if (changeForm.UpdateNow)
                InstallNow();
        }

        void ViewError_Click(object sender, EventArgs e)
        {
            frmError errorForm = new frmError(errorTitle, errorMessage, translation);
            errorForm.ShowDialog(ownerForm);

            if (errorForm.TryAgainLater)
                TryAgainLater_Click(this, EventArgs.Empty);
        }

        void TryAgainLater_Click(object sender, EventArgs e)
        {
            // we'll check on next start of this app,
            // just hide for now.
            Hide();
        }

        void TryAgainNow_Click(object sender, EventArgs e)
        {
            // check for updates (if we're actually further along, wyUpdate will set us straight)
            ForceCheckForUpdate(false);
        }


        void CreateMenu(MenuType NewMenuType)
        {
            // if the context menu is visible
            if (isMenuVisible)
            {
                // hide the context menu
                SendKeys.Send("{ESC}");
            }


            // destroy previous menu type (by removing events associated with it)
            // unregister the events to existing menu items
            switch (CurrMenuType)
            {
                case MenuType.CancelDownloading:
                case MenuType.CancelExtracting:
                case MenuType.CheckingMenu:
                    contextMenu.MenuItems[0].Click -= CancelUpdate_Click;
                    break;

                case MenuType.InstallAndChanges:
                case MenuType.DownloadAndChanges:
                    contextMenu.MenuItems[0].Click -= InstallNow_Click;
                    contextMenu.MenuItems[1].Click -= ViewChanges_Click;
                    break;

                case MenuType.Error:
                    contextMenu.MenuItems[0].Click -= TryAgainLater_Click;
                    contextMenu.MenuItems[1].Click -= TryAgainNow_Click;
                    contextMenu.MenuItems[3].Click -= ViewError_Click;
                    break;

                case MenuType.UpdateSuccessful:
                    contextMenu.MenuItems[0].Click -= Hide_Click;
                    contextMenu.MenuItems[1].Click -= ViewChanges_Click;
                    break;

                case MenuType.AlreadyUpToDate:
                    contextMenu.MenuItems[0].Click -= Hide_Click;
                    break;
            }


            // create new menu type & add new events

            switch (NewMenuType)
            {
                case MenuType.Nothing:

                    contextMenu = null;

                    break;
                case MenuType.CheckingMenu:
                    contextMenu = new ContextMenu(new[] { new MenuItem(translation.StopChecking, CancelUpdate_Click) });
                    break;
                case MenuType.CancelDownloading:
                    contextMenu = new ContextMenu(new[] { new MenuItem(translation.StopDownloading, CancelUpdate_Click) });
                    break;
                case MenuType .CancelExtracting:
                    contextMenu = new ContextMenu(new[] { new MenuItem(translation.StopExtracting, CancelUpdate_Click) });
                    break;
                case MenuType.InstallAndChanges:
                case MenuType.DownloadAndChanges:

                    contextMenu = new ContextMenu(new[]
                                { 
                                    new MenuItem(NewMenuType == MenuType.InstallAndChanges ? translation.InstallUpdateMenu : translation.DownloadUpdateMenu, InstallNow_Click), 
                                    new MenuItem(translation.ViewChangesMenu.Replace("%version%", version), ViewChanges_Click)
                                });
                    contextMenu.MenuItems[0].DefaultItem = true;
                    ShowButtonUpdateNow = true;

                    break;
                case MenuType.Error:

                    contextMenu = new ContextMenu(new[]
                                { 
                                    new MenuItem(translation.TryAgainLater, TryAgainLater_Click), 
                                    new MenuItem(translation.TryAgainNow, TryAgainNow_Click),
                                    new MenuItem("-"),
                                    new MenuItem(translation.ViewError, ViewError_Click)
                                });
                    contextMenu.MenuItems[0].DefaultItem = true;
                    
                    break;
                case MenuType.UpdateSuccessful:

                    contextMenu = new ContextMenu(new[]
                                {
                                    new MenuItem(translation.HideMenu, Hide_Click),
                                    new MenuItem(translation.ViewChangesMenu.Replace("%version%", version), ViewChanges_Click)
                                });

                    ShowButtonUpdateNow = false;

                    break;

                case MenuType.AlreadyUpToDate:

                    contextMenu = new ContextMenu(new[]
                                {
                                    new MenuItem(translation.HideMenu, Hide_Click)
                                });
                    break;
            }

            CurrMenuType = NewMenuType;
        }


        void tmrWaitBeforeCheck_Tick(object sender, EventArgs e)
        {
            forceCheck(false, sender == null);
        }

        void forceCheck(bool recheck, bool forceShow)
        {
            // disable any scheduled checking
            tmrWaitBeforeCheck.Enabled = false;

            BeforeArgs bArgs = new BeforeArgs();

            SetMenuText(translation.CancelCheckingMenu);

            if (BeforeChecking != null)
                BeforeChecking(this, bArgs);

            if (bArgs.Cancel)
            {
                // close wyUpdate
                updateHelper.Cancel();
                return;
            }

            // show the working animation
            SetUpdateStepOn(UpdateStepOn.Checking);
            UpdateProcessing(forceShow);

            // setup the context menu
            CreateMenu(MenuType.CheckingMenu);

            if (recheck)
                updateHelper.ForceRecheckForUpdate();
            else
                updateHelper.CheckForUpdate();
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
            // send the client the arguments that need to run on succes and failure
            updateHelper.RestartInfo(Application.ExecutablePath, AutoUpdaterInfo.AutoUpdateID, Arguments);
        }

        void DownloadUpdate()
        {
            BeforeArgs bArgs = new BeforeArgs();

            SetMenuText(translation.CancelUpdatingMenu);

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

                // set the working progress animation
                UpdateProcessing(false);
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
                errorTitle = translation.PrematureExitTitle;
                errorMessage = translation.PrematureExitMessage;

                UpdateStepFailed(UpdateStepOn);
            }
        }

        void updateHelper_ProgressChanged(object sender, UpdateHelperData e)
        {
            switch (e.ResponseType)
            {
                case Response.Failed:

                    errorTitle = e.ExtraData[0];
                    errorMessage = e.ExtraData[1];

                    // show the error icon & menu
                    // and set last successful step
                    UpdateStepFailed(
                        UpdateStepToUpdateStepOn(e.UpdateStep)
                        );
                    
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

                    // update progress status (only for greater than 0%)
                    
                    if(e.Progress > 0)
                        Text = currentActionText + ", " + e.Progress + "%";

                    // call the progress changed event
                    if (ProgressChanged != null)
                        ProgressChanged(this, e.Progress);

                    break;
            }
        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (RestartInfoSent)
            {
                // show client & send the "begin update" message
                updateHelper.InstallNow();
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
                    ExtractUpdate();

                    break;
                case UpdateStep.BeginExtraction:

                    // inform the user that the update is ready to be installed
                    UpdateReadyToInstall();

                    break;
            }
        }


        void RefreshTextRect()
        {
            textRect = new Rectangle(new Point(20, 0), TextRenderer.MeasureText(Text, Font, Size, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix));

            expandedWidth = textRect.Width + textRect.X;

            if (isFullExpanded && Width != expandedWidth)
            {
                // reposition an resize the control
                if ((Anchor & AnchorStyles.Right) == AnchorStyles.Right)
                    Left -= expandedWidth - Width;

                Width += expandedWidth - Width;
            }
                // if expanding
            else if(tmrAniExpandCollapse.Enabled && sizeChange > 0)
            {
                // re-start the expansion with the new size
                BeginAniOpen();
            }

            // 8  = 16 / 2
            textRect.Y = 8 - textRect.Height / 2 - 1;
        }


        protected override void OnFontChanged(EventArgs e)
        {
            RefreshTextRect();

            Height = Math.Max(16, Font.Height);

            base.OnFontChanged(e);
        }

        void UpdateReady()
        {
            CreateMenu(MenuType.DownloadAndChanges);

            SetUpdateStepOn(UpdateStepOn.UpdateAvailable);

            if (!KeepHidden)
                Show();

            // temporarily diable the collapse timer
            tmrCollapse.Enabled = false;

            // animate this open
            BeginAniOpen();

            AnimateImage(BmpNotify, true);

            SetMenuText(translation.DownloadUpdateMenu);

            if (UpdateAvailable != null)
                UpdateAvailable(this, EventArgs.Empty);
        }

        void UpdateReadyToInstall()
        {
            CreateMenu(MenuType.InstallAndChanges);

            SetUpdateStepOn(UpdateStepOn.UpdateReadyToInstall);

            if (!KeepHidden)
                Show();

            // temporarily diable the collapse timer
            tmrCollapse.Enabled = false;

            // animate this open
            BeginAniOpen();

            AnimateImage(BmpInfo, true);

            SetMenuText(translation.InstallUpdateMenu);

            if (ReadyToBeInstalled != null)
                ReadyToBeInstalled(this, EventArgs.Empty);
        }

        void UpdateStepSuccessful(MenuType menuType)
        {
            // create the "hide" menu
            CreateMenu(menuType);

            // temporarily diable the collapse timer
            tmrCollapse.Enabled = false;

            // animate this open
            BeginAniOpen();

            AnimateImage(BmpSuccess, true);
        }

        void AlreadyUpToDate()
        {
            UpdateStepOn = UpdateStepOn.Nothing;

            Text = translation.AlreadyUpToDate;

            if (Visible)
                UpdateStepSuccessful(MenuType.AlreadyUpToDate);

            SetMenuText(translation.CheckForUpdatesMenu);

            if (UpToDate != null)
                UpToDate(this, new SuccessArgs { Version = version });
        }

        void UpdateStepFailed(UpdateStepOn us)
        {
            //only show the error if this is visible
            if (Visible)
            {
                CreateMenu(MenuType.Error);

                AnimateImage(BmpFailed, true);
            }

            SetLastSuccessfulStep();

            FailArgs failArgs = new FailArgs {ErrorTitle = errorTitle, ErrorMessage = errorMessage};

            SetMenuText(translation.CheckForUpdatesMenu);

            switch (us)
            {
                case UpdateStepOn.Checking:

                    Text = translation.FailedToCheck;

                    if (CheckingFailed != null)
                        CheckingFailed(this, failArgs);

                    break;
                case UpdateStepOn.DownloadingUpdate:

                    Text = translation.FailedToDownload;

                    if (DownloadingOrExtractingFailed != null)
                        DownloadingOrExtractingFailed(this, failArgs);

                    break;
                case UpdateStepOn.ExtractingUpdate:

                    Text = translation.FailedToExtract;

                    if (DownloadingOrExtractingFailed != null)
                        DownloadingOrExtractingFailed(this, failArgs);

                    break;
            }
        }

        static UpdateStepOn UpdateStepToUpdateStepOn(UpdateStep us)
        {
            switch(us)
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

        void UpdateProcessing(bool forceShow)
        {
            if (forceShow && !KeepHidden)
                Show();

            // temporarily diable the collapse timer
            tmrCollapse.Enabled = false;

            // animate this open
            BeginAniOpen();

            AnimateImage(BmpWorking, false);
        }

        void AnimateImage(Image img, bool staticImg)
        {
            ani.StopAnimation();
            ani.StaticImage = staticImg;
            ani.AnimationInterval = 25;
            ani.BaseImage = img;
            ani.StartAnimation();
        }

        void SetUpdateStepOn(UpdateStepOn uso)
        {
            UpdateStepOn = uso;

            switch(uso)
            {
                case UpdateStepOn.Checking:
                    Text = currentActionText = translation.Checking;
                    break;

                case UpdateStepOn.DownloadingUpdate:
                    Text = currentActionText = translation.Downloading;
                    break;

                case UpdateStepOn.ExtractingUpdate:
                    Text = currentActionText = translation.Extracting;
                    break;

                case UpdateStepOn.UpdateAvailable:
                    Text = translation.UpdateAvailable;
                    break;

                case UpdateStepOn.UpdateReadyToInstall:
                    Text = internalUpdateType == UpdateType.CheckAndDownload
                        ? translation.UpdateAvailable
                        : translation.InstallOnNextStart;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
            e.Graphics.InterpolationMode = InterpolationMode.Low;

            e.Graphics.FillRectangle(backBrush, ClientRectangle);

            //Add the text
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);

            backBrush = new SolidBrush(BackColor);
        }

        protected override void WndProc(ref Message m)
        {
            //0x0212 == WM_EXITMENULOOP
            if (m.Msg == 0x212)
            {
                //this message is only sent when a ContextMenu is closed (not a ContextMenuStrip)
                isMenuVisible = false;

                //begin collapsing the update helper
                if (isFullExpanded || tmrAniExpandCollapse.Enabled)
                    tmrCollapse.Enabled = true;
            }

            base.WndProc(ref m);
        }

        void ISupportInitialize.BeginInit() { }

        void ISupportInitialize.EndInit()
        {
            if (DesignMode)
                return;

            SetMenuText(translation.CheckForUpdatesMenu);

            // read settings file for last check time
            AutoUpdaterInfo = new AutoUpdaterInfo(m_GUID, null);

            // see if update is pending, if so force install
            if (AutoUpdaterInfo.UpdateStepOn == UpdateStepOn.UpdateReadyToInstall)
            {
                //TODO: test funky non-compliant state file

                // hide self if there's an update pending
                ownerForm.ShowInTaskbar = false;
                ownerForm.WindowState = FormWindowState.Minimized;

                // then KillSelf&StartUpdater
                ClosingForInstall = true;

                // start the updater
                InstallPendingUpdate();
            }
            else
            {
                // get the current update step from the 
                m_UpdateStepOn = AutoUpdaterInfo.UpdateStepOn;

                if (UpdateStepOn != UpdateStepOn.Nothing)
                {
                    version = AutoUpdaterInfo.UpdateVersion;
                    changes = AutoUpdaterInfo.ChangesInLatestVersion;
                    changesAreRTF = AutoUpdaterInfo.ChangesIsRTF;

                    switch (UpdateStepOn)
                    {
                        case UpdateStepOn.UpdateAvailable:
                            UpdateReady();
                            break;

                        case UpdateStepOn.UpdateReadyToInstall:
                            UpdateReadyToInstall();
                            break;

                        case UpdateStepOn.UpdateDownloaded:

                            // show the updater control
                            if (!KeepHidden)
                                Show();

                            // begin extraction
                            ExtractUpdate();
                            break;
                    }
                }
                else if (AutoUpdaterInfo.AutoUpdaterStatus == AutoUpdaterStatus.UpdateSucceeded)
                {
                    // show the control
                    Visible = !KeepHidden;

                    // set the version & changes
                    version = AutoUpdaterInfo.UpdateVersion;
                    changes = AutoUpdaterInfo.ChangesInLatestVersion;
                    changesAreRTF = AutoUpdaterInfo.ChangesIsRTF;

                    // clear the changes and resave
                    AutoUpdaterInfo.ClearSuccessError();
                    AutoUpdaterInfo.Save();


                    Text = translation.SuccessfullyUpdated.Replace("%version%", version);
                    UpdateStepSuccessful(MenuType.UpdateSuccessful);

                    if (UpdateSuccessful != null)
                        UpdateSuccessful(this, new SuccessArgs { Version = version });
                }
                else if (AutoUpdaterInfo.AutoUpdaterStatus == AutoUpdaterStatus.UpdateFailed)
                {
                    // show the control
                    Visible = !KeepHidden;

                    // fill the errorTitle & errorMessage
                    errorTitle = AutoUpdaterInfo.ErrorTitle;
                    errorMessage = AutoUpdaterInfo.ErrorMessage;

                    // clear the error and resave
                    AutoUpdaterInfo.ClearSuccessError();
                    AutoUpdaterInfo.Save();

                    // show failed Text & icon
                    Text = translation.UpdateFailed;
                    CreateMenu(MenuType.Error);
                    AnimateImage(BmpFailed, true);

                    if (UpdateFailed != null)
                        UpdateFailed(this, new FailArgs { ErrorTitle = errorTitle, ErrorMessage = errorMessage });
                }
                else
                    Visible = false;
            }
        }

        void ownerForm_Load(object sender, EventArgs e)
        {
            if (m_UpdateType != UpdateType.DoNothing)
            {
                // if we want to kill ouself, then don't bother checking for updates
                if (ClosingForInstall)
                    return;

                // see if enough days have elapsed since last check.
                TimeSpan span = DateTime.Now.Subtract(AutoUpdaterInfo.LastCheckedForUpdate);

                if (span.Days >= m_DaysBetweenChecks)
                {
                    tmrWaitBeforeCheck.Enabled = true;
                }
            }
        }
    }
}
