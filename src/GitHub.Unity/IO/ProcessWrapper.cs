using System;
using System.Diagnostics;
using GitHub.Unity.Extensions;

namespace GitHub.Unity
{
    class ProcessWrapper : IProcess
    {
        public event Action<string> OnOutputData;
        public event Action<string> OnErrorData;
        public event Action<IProcess> OnExit;

        private Process process;
        public ProcessWrapper(ProcessStartInfo psi)
        {
            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (s, e) =>
            {
                UnityEngine.Debug.Log("Data " + e.Data + " exit?" + process.HasExited + " (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")");
                OnOutputData.SafeInvoke(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                UnityEngine.Debug.Log("Error (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")");
                OnErrorData.SafeInvoke(e.Data);
                if (process.HasExited)
                {
                    OnExit.SafeInvoke(this);
                }
            };
            process.Exited += (s, e) =>
            {
                UnityEngine.Debug.Log("Exit (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")");
                OnExit.SafeInvoke(this);
            };
        }

        public void Run()
        {
            UnityEngine.Debug.Log("Running process (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public bool WaitForExit(int milliseconds)
        {
            UnityEngine.Debug.Log("Waiting " + milliseconds + " (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")");

            // Workaround for a bug in which some data may still be processed AFTER this method returns true, thus losing the data.
            // http://connect.microsoft.com/VisualStudio/feedback/details/272125/waitforexit-and-waitforexit-int32-provide-different-and-undocumented-implementations
            bool waitSucceeded = process.WaitForExit(milliseconds);
            if (waitSucceeded)
            {
                process.WaitForExit();
            }
            return waitSucceeded;
        }

        public void WaitForExit()
        {
            process.WaitForExit();
        }

        public void Close()
        {
            process.Close();
        }

        public void Kill()
        {
            process.Kill();
        }

        void OnDataReceived(DataReceivedEventHandler handler, DataReceivedEventArgs e)
        {
            handler.Invoke(this, e);
        }

        public int Id { get { return process.Id; } }

        public bool HasExited { get { return process.HasExited; } }
    }
}