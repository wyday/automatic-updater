using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace WindowsService
{
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        /// <summary>Public Constructor for WindowsServiceInstaller.</summary>
        public WindowsServiceInstaller()
        {
            ServiceProcessInstaller spInstaller = new ServiceProcessInstaller
                                                      {
                                                          Account = ServiceAccount.LocalSystem,
                                                          Username = null,
                                                          Password = null
                                                      };

            ServiceInstaller sInstaller = new ServiceInstaller
                                              {
                                                  DisplayName = "Test AutoUpdate Service in C#",
                                                  Description = "A simple service that writes to \"C:\\NETWinService.txt\"",
                                                  StartType = ServiceStartMode.Manual,

                                                  // This must be identical to the WindowsService.ServiceBase name
                                                  // set in the constructor of WindowsService.cs
                                                  ServiceName = "Test AutoUpdate Service"
                                              };

            Installers.Add(spInstaller);
            Installers.Add(sInstaller);
        }
    }
}
