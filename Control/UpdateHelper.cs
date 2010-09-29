using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using wyUpdate.Common;

#if WPF
using System.Windows;
using System.Windows.Threading;
#else
using System.Windows.Forms;
#endif


namespace wyDay.Controls
{
    internal partial class UpdateHelper
    {
        // Constants
        const int MaxSendRetries = 20;
        const int MilliSecsBetweenRetry = 250;

        // 20 * 250 = 5000 ms = 5 seconds for an error to show

        Process ClientProcess;

        public string wyUpdateLocation;

        // extra arguments for wyUpdate
        public string ExtraArguments;

        PipeClient pipeClient;


        readonly BackgroundWorker bw = new BackgroundWorker();

        // current process ID the client can use to respond
#if WPF
        readonly UIElement OwnerElement;
#else
        readonly Form MainWindow;
#endif
        

        public event UpdateStepMismatchHandler UpdateStepMismatch;
        public event ResponseHandler ProgressChanged;
        public event EventHandler PipeServerDisconnected;

        public UpdateStep UpdateStep = UpdateStep.CheckForUpdate;

        [DllImport("User32")]
        static extern int ShowWindow(int hwnd, int nCmdShow);

        
        // for messages sent before the handle has been created
        UpdateHelperData bufferedUHD;

        readonly Stack<UpdateHelperData> uhdStack = new Stack<UpdateHelperData>(1);

#if WPF
        public UpdateHelper(UIElement OwnerElement)
        {
            this.OwnerElement = OwnerElement;

            CreateNewPipeClient();

            bw.DoWork += bw_DoWork;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
        }
#else // Windows Forms
        public UpdateHelper(Form OwnerForm)
        {
            MainWindow = OwnerForm;

            // if the handle for OwnerForm has already been created, just setup the pipeserver
            if (OwnerForm.IsHandleCreated)
            {
                CreateNewPipeClient();
            }
            else
                OwnerForm.HandleCreated += OwnerForm_HandleCreated;

            bw.DoWork += bw_DoWork;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
        }

        void OwnerForm_HandleCreated(object sender, EventArgs e)
        {
            // remove the event - no longer needed
            MainWindow.HandleCreated -= OwnerForm_HandleCreated;

            // setup the pipe client
            CreateNewPipeClient();

            // send any bufferedUHD
            if (bufferedUHD != null)
                SendAsync(bufferedUHD);
        }
#endif

        void CreateNewPipeClient()
        {
            if (pipeClient != null)
            {
                pipeClient.MessageReceived -= pipeClient_MessageReceived;
                pipeClient.ServerDisconnected -= pipeClient_ServerDisconnected;
            }

            pipeClient = new PipeClient();

            pipeClient.MessageReceived += pipeClient_MessageReceived;
            pipeClient.ServerDisconnected += pipeClient_ServerDisconnected;
        }

        bool StartClient()
        {
            // get the unique pipe name (the last 246 chars of the complete path)
            string pipeName = UpdateHelperData.PipenameFromFilename(wyUpdateLocation);
            
            // first try to connect to the pipe
            pipeClient.Connect(pipeName);

            if (pipeClient.Connected)
            {
                // request the processId
                if (!RetrySend((new UpdateHelperData(UpdateAction.GetwyUpdateProcessID)).GetByteArray()))
                    throw new Exception("Failed to get the wyUpdate ProcessID.");

                return true;
            }

            ClientProcess = new Process
                                {
                                    StartInfo =
                                        {
                                            FileName = wyUpdateLocation,
                                            
                                            // start the client in automatic update mode (a.k.a. wait mode)
                                            Arguments = "/autoupdate",

                                            WindowStyle = ProcessWindowStyle.Hidden
                                        }
                                };

            if (!string.IsNullOrEmpty(ExtraArguments))
                ClientProcess.StartInfo.Arguments += " " + ExtraArguments;

            ClientProcess.Start();

            TryToConnectToPipe(pipeName);

            // if the pipe couldn't connect, bail out and fail
            if (!pipeClient.Connected)
            {
                ClientProcess.Kill();
                ClientProcess = null;
                return false;
            }

            return true;
        }

        void TryToConnectToPipe(string pipename)
        {
            // try to connect to the pipe - bail out if it takes longer than 30 seconds
            for (int retries = 0; !pipeClient.Connected && retries < 60; retries++)
            {
                pipeClient.Connect(pipename);

                // wait half a second
                if (!pipeClient.Connected)
                {
                    if (ClientProcess == null || ClientProcess.HasExited)
                    {
                        ClientProcess = null;
                        break;
                    }

                    Thread.Sleep(500);
                }
            }
        }

        void pipeClient_ServerDisconnected()
        {
            try
            {
#if WPF
                if (OwnerElement.Dispatcher.CheckAccess())
                {
                    // The calling thread owns the dispatcher, and hence the UI element
                    ServerDisconnected();
                }
                else
                {
                    // Invocation required
                    OwnerElement.Dispatcher.Invoke(DispatcherPriority.Normal, new PipeClient.ServerDisconnectedHandler(ServerDisconnected));
                }
#else
                // don't recieve messages after the main window has closed
                if (MainWindow.IsDisposed)
                    return;

                MainWindow.Invoke(new PipeClient.ServerDisconnectedHandler(ServerDisconnected));
#endif
            }
            catch { }
        }

        void ServerDisconnected()
        {
            ClientProcess = null;

            if (PipeServerDisconnected != null)
                PipeServerDisconnected(this, EventArgs.Empty);
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            // start the client if it's not already running.
            if (ClientProcess == null)
            {
                if (!StartClient())
                    throw new Exception("Updater client failed to start.");
            }

            if (!RetrySend(((UpdateHelperData)e.Argument).GetByteArray()))
                throw new Exception("Message failed to send message to pipe server");
        }

        /// <summary>
        /// Tries to send a message MaxSendRetries waiting MilliSecsBetweenRetry.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <returns>True if success.</returns>
        bool RetrySend(byte[] message)
        {
            bool messageFailedToSend;

            for (int retries = 0;

                // try to send the message
                (messageFailedToSend = !pipeClient.SendMessage(message))
                    && retries < MaxSendRetries;

                retries++)
            {
                // wait between retries
                Thread.Sleep(MilliSecsBetweenRetry);
            }

            return !messageFailedToSend;
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // error occurs when a message fails to send or wyUpdate fails to start
            if (e.Error != null)
            {
                // if the process is running - try to kill it
                try
                {
                    if (ClientProcess != null && !ClientProcess.HasExited)
                        ClientProcess.Kill();
                }
                catch { }

                //clear the to-send stack
                uhdStack.Clear();

                // inform the AutomaticUpdater that wyUpdate is no longer running
                if (PipeServerDisconnected != null)
                    PipeServerDisconnected(this, EventArgs.Empty);
            }
            else
            {
                // process the next in stack
                if (uhdStack.Count > 0)
                {
                    UpdateHelperData uhd = uhdStack.Pop();
                    UpdateStep = uhd.UpdateStep;

                    // begin sending to the client
                    bw.RunWorkerAsync(uhd);
                }
            }
        }

        public void ForceRecheckForUpdate()
        {
            SendAsync(new UpdateHelperData(UpdateStep.ForceRecheckForUpdate));
        }

        public void CheckForUpdate()
        {
            SendAsync(new UpdateHelperData(UpdateStep.CheckForUpdate));
        }

        public void DownloadUpdate()
        {
            SendAsync(new UpdateHelperData(UpdateStep.DownloadUpdate));
        }

        public void BeginExtraction()
        {
            SendAsync(new UpdateHelperData(UpdateStep.BeginExtraction));
        }

        public void RestartInfo(string fileToExecute, string autoUpdateID, string argumentsForFiles)
        {
            UpdateHelperData uhd = new UpdateHelperData(UpdateStep.RestartInfo);

            uhd.ExtraData.Add(fileToExecute);
            uhd.ExtraDataIsRTF.Add(false);

            if (!string.IsNullOrEmpty(autoUpdateID))
            {
                uhd.ExtraData.Add(autoUpdateID);
                uhd.ExtraDataIsRTF.Add(false);

                if (!string.IsNullOrEmpty(argumentsForFiles))
                {
                    uhd.ExtraData.Add(argumentsForFiles);
                    uhd.ExtraDataIsRTF.Add(false);
                }
            }

            SendAsync(uhd);
        }

        public void InstallNow()
        {
            // get the updater's window handle
            GetClientWindowHandle();

            // show & set as foreground window ( SW_RESTORE = 9)
            ShowWindow((int)ClientMainWindowHandle, 9);
            SetForegroundWindow(ClientMainWindowHandle);

            //begin installing the update
            SendAsync(new UpdateHelperData(UpdateStep.Install));
        }

        public void Cancel()
        {
            SendAsync(new UpdateHelperData(UpdateAction.Cancel));
        }

        void SendAsync(UpdateHelperData uhd)
        {
            // pipeClient is only null when the handle for the form hasn't been created yet
            if (pipeClient == null)
            {
                // buffer the message to send
                bufferedUHD = uhd;
                return;
            }

            // if currently working, add the new message to the stack
            if (bw.IsBusy)
            {
                uhdStack.Push(uhd);
            }
            else
            {
                // process immediately
                UpdateStep = uhd.UpdateStep == UpdateStep.ForceRecheckForUpdate ? UpdateStep.CheckForUpdate : uhd.UpdateStep;

                // begin sending to the client
                bw.RunWorkerAsync(uhd);
            }
        }

        void pipeClient_MessageReceived(byte[] message)
        {
            // TODO: remove try-catch block OR handle the error some other way
            try
            {
#if WPF
                if (OwnerElement.Dispatcher.CheckAccess())
                {
                    // The calling thread owns the dispatcher, and hence the UI element
                    ProcessReceivedMessage(message);
                }
                else
                {
                    // Invocation required
                    OwnerElement.Dispatcher.Invoke(DispatcherPriority.Normal, new PipeClient.MessageReceivedHandler(ProcessReceivedMessage), message);
                }
#else
                // don't recieve messages after the main window has closed
                if (MainWindow.IsDisposed)
                    return;

                MainWindow.Invoke(new PipeClient.MessageReceivedHandler(ProcessReceivedMessage),
                                  new object[] {message});
#endif
            }
            catch { }
        }

        void ProcessReceivedMessage(byte[] message)
        {
            // Cast the data to the type of object we sent:
            UpdateHelperData data = UpdateHelperData.FromByteArray(message);

            if (data.Action == UpdateAction.GetwyUpdateProcessID)
            {
                ClientProcess = Process.GetProcessById(data.ProcessID);
                return;
            }
            
            if (data.Action == UpdateAction.NewWyUpdateProcess)
            {
                // disconnect from the existing pipeclient
                pipeClient.Disconnect();
                
                CreateNewPipeClient();
                
                ClientProcess = Process.GetProcessById(data.ProcessID);

                TryToConnectToPipe(data.ExtraData[0]);

                // if the process is running - try to kill it
                if (!pipeClient.Connected)
                {
                    try
                    {
                        if (!ClientProcess.HasExited)
                            ClientProcess.Kill();
                    }
                    catch { }

                    // inform the AutomaticUpdater that wyUpdate is no longer running
                    if (PipeServerDisconnected != null)
                        PipeServerDisconnected(this, EventArgs.Empty);
                }

                // begin where we left off
                // if update step == RestartInfo, we need to send the restart info as well
                if (bufferedUHD != null && UpdateStep == bufferedUHD.UpdateStep)
                    SendAsync(bufferedUHD);
                else
                    SendAsync(new UpdateHelperData(UpdateStep));

                return;
            }

            if (data.UpdateStep != UpdateStep)
            {
                // this occurs when wyUpdate is on a separate step from the AutoUpdater

                UpdateStep prev = UpdateStep;

                // set new update step
                UpdateStep = data.UpdateStep;

                // tell AutoUpdater that the message we sent didn't respond in kind
                // e.g. we sent RestartInfo, and wyUpdate responded with DownloadUpdate
                // meaning we can't update yet, we're just begginning downloading the update
                if (UpdateStepMismatch != null)
                    UpdateStepMismatch(this, data.ResponseType, prev);
            }

            if (data.ResponseType != Response.Nothing && ProgressChanged != null)
                ProgressChanged(this, data);
        }
    }

    internal delegate void ResponseHandler(object sender, UpdateHelperData e);
    internal delegate void UpdateStepMismatchHandler(object sender, Response respType, UpdateStep previousStep);
}
