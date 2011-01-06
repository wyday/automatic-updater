using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using wyDay.Controls;

namespace WindowsService
{
    class WindowsService : ServiceBase
    {
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
        /// Dispose of objects that need it here.
        /// </summary>
        /// <param name="disposing">Whether or not disposing is going on.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        [Conditional("DEBUG")]
        private static void DebugMode()
        {
            Debugger.Break();
        }

        static AutomaticUpdaterBackend auBackend;
        static readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        /// <summary>
        /// OnStart: Put startup code here
        ///  - Start threads, get inital data, etc.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            //Note: DebugMode() is so you can attach a debugger (Visual Studio) to
            //      to this process.
            DebugMode();

            auBackend = new AutomaticUpdaterBackend
                            {
                                //TODO: set a unique string. For instance, "appname-companyname"
                                GUID = "a-string-that-uniquely-IDs-your-service",
                                UpdateType = UpdateType.Automatic,
                                ServiceName = ServiceName
                            };

            auBackend.ReadyToBeInstalled += auBackend_ReadyToBeInstalled;
            auBackend.UpdateSuccessful += auBackend_UpdateSuccessful;

            //TODO: use the failed events for loggin (CheckingFailed, DownloadingFailed, ExtractingFailed, UpdateFailed)

            // the function to be called after all events have been set.
            auBackend.Initialize();
            auBackend.AppLoaded();

            //TODO: only ForceCheckForUpdate() every N days!
            // You don't want to recheck for updates on every app start.
            if (auBackend.UpdateStepOn == UpdateStepOn.Nothing)
                auBackend.ForceCheckForUpdate();

            // Blocks until "resetEvent.Set()" on another thread
            resetEvent.WaitOne();
            base.OnStart(args);
        }

        static void auBackend_UpdateSuccessful(object sender, SuccessArgs e)
        {
            Console.WriteLine("Successfully updated to version " + e.Version);
            Console.WriteLine("Changes: ");
            Console.WriteLine(auBackend.Changes);
        }

        static void auBackend_ReadyToBeInstalled(object sender, EventArgs e)
        {
            if (auBackend.UpdateStepOn == UpdateStepOn.UpdateReadyToInstall)
            {
                //TODO: delay the installation of the update until it's appropriate for your app.

                //TODO: do any "spin-down" operations. auBackend.InstallNow() will exit this process, so run any cleanup functions now.

                // here we'll just close immediately to install the new version
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
