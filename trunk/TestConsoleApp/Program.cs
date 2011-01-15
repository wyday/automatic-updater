using System;
using System.Threading;
using wyDay.Controls;

namespace TestConsoleApp
{
    class Program
    {
        static AutomaticUpdaterBackend auBackend;
        static readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            auBackend = new AutomaticUpdaterBackend
                            {
                                //TODO: set a unique string. For instance, "appname-companyname"
                                GUID = "a-string-that-uniquely-IDs-your-app",

                                // With UpdateType set to Automatic, you're still in charge of
                                // checking for updates, but the AutomaticUpdaterBackend
                                // continues with the downloading and extracting automatically.
                                UpdateType = UpdateType.Automatic
                            };

            auBackend.ReadyToBeInstalled += auBackend_ReadyToBeInstalled;
            auBackend.UpdateSuccessful += auBackend_UpdateSuccessful;

            //TODO: use the failed events for logging (CheckingFailed, DownloadingFailed, ExtractingFailed, UpdateFailed)

            // the functions to be called after all events have been set.
            auBackend.Initialize();
            auBackend.AppLoaded();

            // sees if you checked in the last 10 days, if not it rechecks
            CheckEvery10Days();

            // Blocks until "resetEvent.Set()" on another thread
            resetEvent.WaitOne();
        }

        static void CheckEvery10Days()
        {
            // Only ForceCheckForUpdate() every N days!
            // You don't want to recheck for updates on every app start.

            if ((DateTime.Now - auBackend.LastCheckDate).TotalDays > 9
                && auBackend.UpdateStepOn == UpdateStepOn.Nothing)
            {
                auBackend.ForceCheckForUpdate();
            }
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
                //TODO: Delay the installation of the update until it's appropriate for your app.

                //TODO: Do any "spin-down" operations. auBackend.InstallNow() will
                //      exit this process using Environment.Exit(0), so run
                //      cleanup functions now (close threads, close running programs, release locked files, etc.)

                // here we'll just close immediately to install the new version
                auBackend.InstallNow();
            }
        }
    }
}
