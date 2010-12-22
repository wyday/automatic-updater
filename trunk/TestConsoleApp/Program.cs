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
                                GUID = "a-string-that-uniquely-IDs-you-app",
                                UpdateType = UpdateType.Automatic
                            };

            auBackend.ReadyToBeInstalled += auBackend_ReadyToBeInstalled;
            auBackend.UpdateSuccessful += auBackend_UpdateSuccessful;

            //TODO: use the failed events for loggin (CheckingFailed, DownloadingFailed, ExtractingFailed, UpdateFailed)

            // the function to be called after all events have been set.
            auBackend.Initialize();
            auBackend.AppLoaded();

            if (auBackend.UpdateStepOn == UpdateStepOn.Nothing)
                auBackend.ForceCheckForUpdate();

            // Blocks until "resetEvent.Set()" on another thread
            resetEvent.WaitOne();
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

                // do any "spin-down" operations. auBackend.InstallNow() will exit this process, so run any cleanup functions now.

                // here we'll just close immediately to install the new version
                auBackend.InstallNow();
            }
        }
    }
}
