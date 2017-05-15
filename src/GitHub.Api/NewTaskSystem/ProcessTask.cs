using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class ProcessTaskExtensions
    {
        public static T ConfigureGitProcess<T>(this T task, IProcessManager processManager, bool withInput = false)
            where T : IProcess
        {
            return processManager.ConfigureGitProcess(task, withInput);
        }

        public static T Configure<T>(this T task, IProcessManager processManager, string executable, string arguments, string workingDirectory, bool withInput)
            where T : IProcess
        {
            return processManager.Configure(task, executable, arguments, workingDirectory, withInput);
        }
    }

    interface IProcess
    {
        void Configure(Process existingProcess);
        void Configure(ProcessStartInfo psi);
        event Action<string> OnErrorData;
        StreamWriter StandardInput { get; }
        int ProcessId { get; }
        string ProcessName { get; }
        string ProcessArguments { get; }
        Process Process { get; set; }
        event Action<IProcess> OnStartProcess;
        event Action<IProcess> OnEndProcess;
    }

    interface IProcessTask<T> : ITask<T>, IProcess
    {
        void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor);
    }

    interface IProcessTask<T, TData> : ITask<T, TData>, IProcess
    {
        void Configure(ProcessStartInfo psi, IOutputProcessor<T, TData> processor);
    }

    class ProcessWrapper
    {
        private readonly IOutputProcessor outputProcessor;
        private readonly Action onStart;
        private readonly Action onEnd;
        private readonly Action<Exception, string> onError;
        private readonly CancellationToken token;
        private readonly List<string> errors = new List<string>();

        public Process Process { get; }
        public StreamWriter Input { get; private set; }

        private ILogging logger;
        protected ILogging Logger { get { return logger = logger ?? Logging.GetLogger(GetType()); } }

        public ProcessWrapper(Process process, IOutputProcessor outputProcessor,
            Action onStart, Action onEnd, Action<Exception, string> onError,
            CancellationToken token)
        {
            this.outputProcessor = outputProcessor;
            this.onStart = onStart;
            this.onEnd = onEnd;
            this.onError = onError;
            this.token = token;
            this.Process = process;
        }

        public void Run()
        {
            if (Process.StartInfo.RedirectStandardOutput)
            {
                Process.OutputDataReceived += (s, e) =>
                {
                    //logger.Trace("OutputData \"" + (e.Data == null ? "'null'" : e.Data) + "\" exited:" + process.HasExited);

                    string encodedData = null;
                    if (e.Data != null)
                    {
                        encodedData = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Data));
                    }
                    outputProcessor.LineReceived(encodedData);
                };
            }

            if (Process.StartInfo.RedirectStandardError)
            {
                Process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        //logger.Trace("ErrorData \"" + (e.Data == null ? "'null'" : e.Data) + "\" exited:" + process.HasExited);
                    }

                    string encodedData = null;
                    if (e.Data != null)
                    {
                        encodedData = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Data));
                        errors.Add(encodedData);
                    }
                };
            }

            try
            {
                Process.Start();
            }
            catch (Win32Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error code " + ex.NativeErrorCode);
                if (ex.NativeErrorCode == 2)
                {
                    sb.AppendLine("The system cannot find the file specified.");
                }
                foreach (string env in Process.StartInfo.EnvironmentVariables.Keys)
                {
                    sb.AppendFormat("{0}:{1}", env, Process.StartInfo.EnvironmentVariables[env]);
                    sb.AppendLine();
                }
                onError?.Invoke(ex, String.Format("{0} {1}", ex.Message, sb.ToString()));
                onEnd?.Invoke();
                return;
            }

            if (Process.StartInfo.RedirectStandardOutput)
                Process.BeginOutputReadLine();
            if (Process.StartInfo.RedirectStandardError)
                Process.BeginErrorReadLine();
            if (Process.StartInfo.RedirectStandardInput)
                Input = new StreamWriter(Process.StandardInput.BaseStream, new UTF8Encoding(false));

            onStart?.Invoke();

            if (Process.StartInfo.CreateNoWindow)
            {
                while (!WaitForExit(500))
                {
                    if (token.IsCancellationRequested)
                    {
                        if (!Process.HasExited)
                            Process.Kill();
                        Process.Close();
                        onEnd?.Invoke();
                        token.ThrowIfCancellationRequested();
                    }
                }

                if (Process.ExitCode != 0)
                {
                    onError?.Invoke(null, String.Join(Environment.NewLine, errors.ToArray()));
                }
            }
            onEnd?.Invoke();
        }

        private bool WaitForExit(int milliseconds)
        {
            //logger.Debug("WaitForExit - time: {0}ms", milliseconds);

            // Workaround for a bug in which some data may still be processed AFTER this method returns true, thus losing the data.
            // http://connect.microsoft.com/VisualStudio/feedback/details/272125/waitforexit-and-waitforexit-int32-provide-different-and-undocumented-implementations
            bool waitSucceeded = Process.WaitForExit(milliseconds);
            if (waitSucceeded)
            {
                Process.WaitForExit();
            }
            return waitSucceeded;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T">The type of the results. If it's a List<> or similar, then specify the full List<> type here and the inner type of the List in <typeparam name="TData"/>
    /// <typeparam name="TData">If <typeparam name="TData"/> is a list or similar, then specify its inner type here</typeparam>
    class ProcessTask<T> : TaskBase<T>, IProcessTask<T>
    {
        private IOutputProcessor<T> outputProcessor;
        private ProcessWrapper wrapper;

        public event Action<string> OnErrorData;
        public event Action<IProcess> OnStartProcess;
        public event Action<IProcess> OnEndProcess;

        private string errors = null;
        private Exception thrownException = null;

        public ProcessTask(CancellationToken token)
            : base(token)
        {
        }

        public ProcessTask(CancellationToken token, ITask dependsOn)
            : base(token, dependsOn)
        {
        }

        public ProcessTask(CancellationToken token, IOutputProcessor<T> outputProcessor)
            : base(token)
        {
            this.outputProcessor = outputProcessor;
        }

        public ProcessTask(CancellationToken token, IOutputProcessor<T> outputProcessor, ITask dependsOn)
            : base(token, dependsOn)
        {
            this.outputProcessor = outputProcessor;
        }

        /// <summary>
        /// Process that calls git with the passed arguments
        /// </summary>
        /// <param name="token"></param>
        /// <param name="arguments"></param>
        /// <param name="outputProcessor"></param>
        public ProcessTask(CancellationToken token, string arguments, IOutputProcessor<T> outputProcessor = null, ITask dependsOn = null)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");

            this.outputProcessor = outputProcessor;
            ProcessArguments = arguments;
        }

        public virtual void Configure(ProcessStartInfo psi)
        {
            Guard.ArgumentNotNull(psi, "psi");

            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
        }

        public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor)
        {
            outputProcessor = processor ?? outputProcessor;
            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
        }

        public void Configure(Process existingProcess)
        {
            Guard.ArgumentNotNull(existingProcess, "existingProcess");

            ConfigureOutputProcessor();
            Process = existingProcess;
            ProcessName = existingProcess.StartInfo.FileName;
        }

        protected override void RaiseOnStart()
        {
            base.RaiseOnStart();
            OnStartProcess?.Invoke(this);
        }

        protected override void RaiseOnEnd()
        {
            base.RaiseOnEnd();
            OnEndProcess?.Invoke(this);
        }

        protected virtual void ConfigureOutputProcessor()
        {
        }

        protected override void Run(bool success)
        {
            throw new NotImplementedException();
        }

        protected override T RunWithReturn(bool success)
        {
            if (!success)
            {
                throw DependsOn.Task.Exception.InnerException;
            }

            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            wrapper = new ProcessWrapper(Process, outputProcessor,
                RaiseOnStart,
                () =>
                {
                    RaiseOnEnd();
                    if (errors != null)
                    {
                        OnErrorData?.Invoke(errors);
                        if (thrownException == null)
                            throw new ProcessException(this);
                        else
                            throw thrownException;
                    }
                },
                (ex, error) =>
                {
                    thrownException = ex;
                    errors = error;
                },
                Token);

            wrapper.Run();

            if (outputProcessor != null)
                return outputProcessor.Result;
            if (typeof(T) == typeof(string))
                return (T)(object)(Process.StartInfo.CreateNoWindow ? "Process finished" : "Process running");
            return default(T);
        }

        public Process Process { get; set; }
        public int ProcessId { get { return Process.Id; } }
        public override bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Process.ExitCode == 0; } }
        public StreamWriter StandardInput { get { return wrapper?.Input; } }
        public virtual string ProcessName { get; protected set; }
        public virtual string ProcessArguments { get; }
    }

    class ProcessTaskWithListOutput<T> : ListTaskBase<List<T>, T>, ITask<List<T>, T>, IProcessTask<List<T>, T>
    {
        private IOutputProcessor<List<T>, T> outputProcessor;
        private string errors = null;
        private Exception thrownException = null;
        private ProcessWrapper wrapper;

        public event Action<string> OnErrorData;
        public event Action<IProcess> OnStartProcess;
        public event Action<IProcess> OnEndProcess;

        public ProcessTaskWithListOutput(CancellationToken token)
            : base(token)
        {
        }

        public ProcessTaskWithListOutput(CancellationToken token, IOutputProcessor<List<T>, T> outputProcessor)
            : this(token)
        {
            this.outputProcessor = outputProcessor;
        }

        public ProcessTaskWithListOutput(CancellationToken token, ITask dependsOn)
            : base(token, dependsOn)
        {
        }

        public ProcessTaskWithListOutput(CancellationToken token, IOutputProcessor<List<T>, T> outputProcessor, ITask dependsOn)
            : base(token, dependsOn)
        {
            this.outputProcessor = outputProcessor;
        }

        public virtual void Configure(ProcessStartInfo psi)
        {
            Guard.ArgumentNotNull(psi, "psi");

            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
        }

        public void Configure(Process existingProcess)
        {
            Guard.ArgumentNotNull(existingProcess, "existingProcess");

            ConfigureOutputProcessor();
            Process = existingProcess;
            ProcessName = existingProcess.StartInfo.FileName;
        }

        public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<List<T>, T> processor)
        {
            Guard.ArgumentNotNull(psi, "psi");
            Guard.ArgumentNotNull(processor, "processor");

            outputProcessor = processor ?? outputProcessor;
            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
        }

        protected override void RaiseOnStart()
        {
            base.RaiseOnStart();
            OnStartProcess?.Invoke(this);
        }

        protected override void RaiseOnEnd()
        {
            base.RaiseOnEnd();
            OnEndProcess?.Invoke(this);
        }

        protected virtual void ConfigureOutputProcessor()
        {
            if (outputProcessor == null && (typeof(T) != typeof(string)))
            {
                throw new InvalidOperationException("ProcessTask without an output processor must be defined as IProcessTask<string>");
            }
            outputProcessor.OnEntry += x => RaiseOnData(x);
        }

        protected override List<T> RunWithReturn(bool success)
        {
            if (!success)
            {
                throw DependsOn.Task.Exception.InnerException;
            }

            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            wrapper = new ProcessWrapper(Process, outputProcessor,
                RaiseOnStart,
                () =>
                {
                    RaiseOnEnd();
                    if (errors != null)
                    {
                        OnErrorData?.Invoke(errors);
                        if (thrownException == null)
                            throw new ProcessException(this);
                        else
                            throw thrownException;
                    }
                },
                (ex, error) =>
                {
                    thrownException = ex;
                    errors = error;
                },
                Token);
            wrapper.Run();

            if (outputProcessor != null)
                return outputProcessor.Result;
            return new List<T>();
        }

        public Process Process { get; set; }
        public int ProcessId { get { return Process.Id; } }
        public override bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Process.ExitCode == 0; } }
        public StreamWriter StandardInput { get { return wrapper?.Input; } }
        public virtual string ProcessName { get; protected set; }
        public virtual string ProcessArguments { get; }
    }
}