using System;
using System.Diagnostics;
using GitHub.Unity.Extensions;
using GitHub.Unity.Logging;

namespace GitHub.Unity
{
    class ProcessWrapper : IProcess
    {
        static readonly ILogger Logger = Logging.Logger.GetLogger<ProcessWrapper>();

        public event Action<string> OnOutputData;
        public event Action<string> OnErrorData;
        public event Action<IProcess> OnExit;

        private Process process;
        public ProcessWrapper(ProcessStartInfo psi)
        {
            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (s, e) =>
            {
                Logger.Debug("Output - \"" + e.Data + "\" exited:" + process.HasExited);
                OnOutputData.SafeInvoke(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;

                Logger.Debug("Error");
                OnErrorData.SafeInvoke(e.Data);
                if (process.HasExited)
                {
                    OnExit.SafeInvoke(this);
                }
            };
            process.Exited += (s, e) =>
            {
                Logger.Debug("Exit");
                OnExit.SafeInvoke(this);
            };
        }

        public void Run()
        {
            Logger.Debug("Run");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public bool WaitForExit(int milliseconds)
        {
            Logger.Debug("WaitForExit - time: " + milliseconds + "ms");

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
            Logger.Debug("WaitForExit");
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

        public int Id { get { return process.Id; } }

        public bool HasExited { get { return process.HasExited; } }
    }
}