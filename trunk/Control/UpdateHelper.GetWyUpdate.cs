using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace wyDay.Controls
{
    internal partial class UpdateHelper
    {
        IntPtr ClientMainWindowHandle;

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        void GetClientWindowHandle()
        {
            // Scan for the main window handle:
            // (it can't be read from ClientProcess.MainWindowHandle because the window is hidden)

            for (int retries = 0; ClientMainWindowHandle == IntPtr.Zero && retries < MaxSendRetries; retries++)
            {
                foreach (ProcessThread pt in ClientProcess.Threads)
                {
                    EnumThreadWindows((uint)pt.Id, EnumThreadCallback, IntPtr.Zero);

                    if (ClientMainWindowHandle != IntPtr.Zero)
                        break;
                }

                if (ClientMainWindowHandle == IntPtr.Zero)
                    Thread.Sleep(MilliSecsBetweenRetry);
            }
        }

        bool EnumThreadCallback(IntPtr hWnd, IntPtr lParam)
        {
            StringBuilder sb = new StringBuilder(9);
            GetWindowText(hWnd, sb, sb.Capacity);

            if (sb.ToString() == "wyUpdate")
            {
                //Found it, bail out
                ClientMainWindowHandle = hWnd;

                // stop enumerating windows for this thread
                return false;
            }

            // continue enumerating windows
            return true;
        }
    }
}