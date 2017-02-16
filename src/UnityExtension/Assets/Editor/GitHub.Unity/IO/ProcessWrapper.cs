using System;
using System.Diagnostics;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace GitHub.Unity
{
    class ProcessWrapper : IProcess
    {
        private static readonly ILogging logger = Logging.GetLogger<ProcessWrapper>();

        public event Action<string> OnOutputData;
        public event Action<string> OnErrorData;
        public event Action<IProcess> OnStart;
        public event Action<IProcess> OnExit;

        private Process process;
        private ProcessState state;
        private bool hasOutputData;
        private StreamWriter input;

        public ProcessWrapper(ProcessStartInfo psi)
        {
            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (s, e) =>
            {
                //logger.Trace("OutputData \"" + (e.Data == null ? "'null'" : e.Data) + "\" exited:" + process.HasExited);

                hasOutputData = true;
                if (process.HasExited)
                {
                    state = ProcessState.Finished;
                }
                
                try
                {
                    OnOutputData.SafeInvoke(e.Data);
                }
                catch(Exception ex)
                {
                    logger.Debug(ex);
                }

                if (e.Data == null)
                {
                    Finished();
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    logger.Trace("ErrorData \"" + (e.Data == null ? "'null'" : e.Data) + "\" exited:" + process.HasExited);
                }

                if (process.HasExited)
                {
                    state = ProcessState.Finished;
                }

                try
                {
                    OnErrorData.SafeInvoke(e.Data);
                }
                catch (Exception ex)
                {
                    logger.Debug(ex);
                }

                if (e.Data == null && !hasOutputData)
                {
                    Finished();
                }
            };
            process.Exited += (s, e) =>
            {
                //logger.Trace("Exited");

                if (!hasOutputData)
                {
                    state = ProcessState.Finished;
                    //logger.Debug("Exit");
                    Finished();
                }
            };
        }

        public void Run()
        {
            //logger.Debug("Run");

            try
            {
                process.Start();
                state = ProcessState.Running;
            }
            catch (Win32Exception ex)
            {
                state = ProcessState.Exception;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error code " + ex.NativeErrorCode);
                if (ex.NativeErrorCode == 2)
                {
                    sb.AppendLine("The system cannot find the file specified.");
                }
                foreach (string env in process.StartInfo.EnvironmentVariables.Keys)
                {
                    sb.AppendFormat("{0}:{1}", env, process.StartInfo.EnvironmentVariables[env]);
                    sb.AppendLine();
                }
                OnErrorData.SafeInvoke(String.Format("{0} {1}", ex.Message, sb.ToString()));
                Finished();
                return;
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            input = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));

            OnStart.SafeInvoke(this);
        }

        public bool WaitForExit(int milliseconds)
        {
            //logger.Debug("WaitForExit - time: {0}ms", milliseconds);

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
            //logger.Debug("WaitForExit");
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

        private void Finished()
        {
            if (HasFinished)
            {
                return;
            }

            //logger.Trace("Finished");
            HasFinished = true;
            OnExit.SafeInvoke(this);
        }

        public int Id { get { return process.Id; } }

        public bool HasExited { get { return state == ProcessState.Finished || state == ProcessState.Exception; } }
        public bool HasFinished { get; private set; }
        public bool Successful
        {
            get
            {
                if (!HasExited)
                    return false;
                return state != ProcessState.Exception && process.ExitCode == 0;
            }
        }

        public StreamWriter StandardInput { get { return input; } }

        enum ProcessState
        {
            NotRunning,
            Running,
            Finished,
            Exception
        }
    }
}