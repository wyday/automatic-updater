using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using wyDay.Controls;

namespace WindowsService
{
    class WindowsService : ServiceBase
    {
        //Note: to install this service, simply call:
        // %SystemRoot%\Microsoft.NET\Framework\v2.0.50727\InstallUtil /i WindowsService.exe

        //To uninstall simply call:
        // %SystemRoot%\Microsoft.NET\Framework\v2.0.50727\InstallUtil /u WindowsService.exe

        /// <summary>
        /// Public Constructor for WindowsService.
        /// - Put all of your Initialization code here.
        /// </summary>
        public WindowsService()
        {
            ServiceName = "Test AutoUpdate Service";
            EventLog.Source = "Test AutoUpdate Service";
            EventLog.Log = "Application";
            
            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            CanPauseAndContinue = true;
            CanShutdown = true;
            CanStop = true;

            if (!EventLog.SourceExists("Test AutoUpdate Service"))
                EventLog.CreateEventSource("Test AutoUpdate Service", "Application");
        }

        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        static void Main()
        {
            Run(new WindowsService());
        }

        /// <summary>
        /// Helper function to attach a debugger to the running service.
        /// </summary>
        [Conditional("DEBUG")]
        static void DebugMode()
        {
            if (!Debugger.IsAttached)
                Debugger.Launch();

            Debugger.Break();
        }

        static AutomaticUpdaterBackend auBackend;

        static void WriteToLog(string message, bool append)
        {
            using (StreamWriter outfile = new StreamWriter(@"C:\NETWinService.txt", append))
            {
                outfile.WriteLine(message);
            }
        }

        /// <summary>
        /// OnStart: Put startup code here
        ///  - Start threads, get inital data, etc.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            WriteToLog(".NET Windows Service v1", false);

            auBackend = new AutomaticUpdaterBackend
                            {
                                //TODO: set a unique string. For instance, "appname-companyname"
                                GUID = "a-string-that-uniquely-IDs-your-service",

                                // With UpdateType set to Automatic, you're still in charge of
                                // checking for updates, but the AutomaticUpdaterBackend
                                // continues with the downloading and extracting automatically.
                                UpdateType = UpdateType.Automatic,

                                // We set the service name that will be used by wyUpdate
                                // to restart this service on update success or failure.
                                ServiceName = this.ServiceName
                            };

            auBackend.ReadyToBeInstalled += auBackend_ReadyToBeInstalled;
            auBackend.UpdateSuccessful += auBackend_UpdateSuccessful;
            auBackend.UpToDate += auBackend_UpToDate;
            auBackend.UpdateFailed += auBackend_UpdateFailed;

            //TODO: use the failed events for logging & error reporting:
            // CheckingFailed, DownloadingFailed, ExtractingFailed, UpdateFailed

            // the functions to be called after all events have been set.
            auBackend.Initialize();
            auBackend.AppLoaded();

            // sees if you checked in the last 10 days, if not it rechecks
            CheckEvery10Days();

            //TODO: Also, since this will be a service you should call CheckEvery10Days() from an another thread (rather than just at startup)
        }

        static void CheckEvery10Days()
        {
            // Only ForceCheckForUpdate() every N days!
            // You don't want to recheck for updates on every app start.

            if ((DateTime.Now - auBackend.LastCheckDate).TotalDays > 10
                && auBackend.UpdateStepOn == UpdateStepOn.Nothing)
            {
                auBackend.ForceCheckForUpdate();
            }
        }

        static void auBackend_UpdateFailed(object sender, FailArgs e)
        {
            //TODO: Notify the admin, or however you want to handle the failure
            WriteToLog("Update failed. Reason\r\nTitle: " + e.ErrorTitle + "\r\nMessage: " + e.ErrorMessage, true);
        }

        static void auBackend_UpToDate(object sender, SuccessArgs e)
        {
            WriteToLog("Already up-to-date!", true);
        }

        static void auBackend_UpdateSuccessful(object sender, SuccessArgs e)
        {
            WriteToLog("Successfully updated to version " + e.Version, true);
            WriteToLog("Changes: ", true);
            WriteToLog(auBackend.Changes, true);
        }

        static void auBackend_ReadyToBeInstalled(object sender, EventArgs e)
        {
            // ReadyToBeInstalled event is called when either the UpdateStepOn == UpdateDownloaded or UpdateReadyToInstall
            if (auBackend.UpdateStepOn == UpdateStepOn.UpdateReadyToInstall)
            {
                //TODO: Delay the installation of the update until it's appropriate for your app.

                //TODO: Do any "spin-down" operations. auBackend.InstallNow() will
                //      exit this process using Environment.Exit(0), so run
                //      cleanup functions now (close threads, close running programs, release locked files, etc.)

                // here we'll just close immediately to install the new version
                WriteToLog("About to install the new version.", true);
                auBackend.InstallNow();
            }
        }

        /// <summary>
        /// OnStop: Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();
        }

        /// <summary>
        /// OnPause: Put your pause code here
        /// - Pause working threads, etc.
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
        }

        /// <summary>
        /// OnContinue: Put your continue code here
        /// - Un-pause working threads, etc.
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();
        }

        /// <summary>
        /// OnShutdown(): Called when the System is shutting down
        /// - Put code here when you need special handling
        ///   of code that deals with a system shutdown, such
        ///   as saving special data before shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();
        }

        /// <summary>
        /// OnCustomCommand(): If you need to send a command to your
        ///   service without the need for Remoting or Sockets, use
        ///   this method to do custom methods.
        /// </summary>
        /// <param name="command">Arbitrary Integer between 128 & 256</param>
        protected override void OnCustomCommand(int command)
        {
            //  A custom command can be sent to a service by using this method:
            //#  int command = 128; //Some Arbitrary number between 128 & 256
            //#  ServiceController sc = new ServiceController("NameOfService");
            //#  sc.ExecuteCommand(command);

            base.OnCustomCommand(command);
        }

        /// <summary>
        /// OnPowerEvent(): Useful for detecting power status changes,
        ///   such as going into Suspend mode or Low Battery for laptops.
        /// </summary>
        /// <param name="powerStatus">The Power Broadcase Status (BatteryLow, Suspend, etc.)</param>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        /// <summary>
        /// OnSessionChange(): To handle a change event from a Terminal Server session.
        ///   Useful if you need to determine when a user logs in remotely or logs off,
        ///   or when someone logs into the console.
        /// </summary>
        /// <param name="changeDescription"></param>
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }
    }
}
