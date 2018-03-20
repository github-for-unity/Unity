using GitHub.Logging;
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
        public static T Configure<T>(this T task, IProcessManager processManager, bool withInput)
            where T : IProcess
        {
            return processManager.Configure(task, withInput: withInput);
        }

        public static T Configure<T>(this T task, IProcessManager processManager, string executable = null,
            string arguments = null,
            NPath? workingDirectory = null,
            bool withInput = false)
            where T : IProcess
        {
            return processManager.Configure(task, executable?.ToNPath(), arguments, workingDirectory, withInput);
        }
    }

    public interface IProcess
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

    interface IProcessTask<TData, T> : ITask<TData, T>, IProcess
    {
        void Configure(ProcessStartInfo psi, IOutputProcessor<TData, T> processor);
    }

    class ProcessWrapper
    {
        private readonly string taskName;
        private readonly IOutputProcessor outputProcessor;
        private readonly Action onStart;
        private readonly Action onEnd;
        private readonly Action<Exception, string> onError;
        private readonly CancellationToken token;
        private readonly List<string> errors = new List<string>();

        public Process Process { get; }
        public StreamWriter Input { get; private set; }

        private ILogging logger;
        protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }

        public ProcessWrapper(string taskName, Process process, IOutputProcessor outputProcessor,
            Action onStart, Action onEnd, Action<Exception, string> onError,
            CancellationToken token)
        {
            this.taskName = taskName;
            this.outputProcessor = outputProcessor;
            this.onStart = onStart;
            this.onEnd = onEnd;
            this.onError = onError;
            this.token = token;
            this.Process = process;
        }

        public void Run()
        {
            Exception thrownException = null;
            if (Process.StartInfo.RedirectStandardError)
            {
                Process.ErrorDataReceived += (s, e) =>
                {
                    //if (e.Data != null)
                    //{
                    //    Logger.Trace("ErrorData \"" + (e.Data == null ? "'null'" : e.Data) + "\"");
                    //}

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
                Logger.Trace($"Running '{Process.StartInfo.FileName} {taskName}'");

                Process.Start();

                if (Process.StartInfo.RedirectStandardInput)
                    Input = new StreamWriter(Process.StandardInput.BaseStream, new UTF8Encoding(false));
                if (Process.StartInfo.RedirectStandardError)
                    Process.BeginErrorReadLine();

                onStart?.Invoke();

                if (Process.StartInfo.RedirectStandardOutput)
                {
                    var outputStream = Process.StandardOutput;
                    var line = outputStream.ReadLine();
                    while (line != null)
                    {
                        outputProcessor.LineReceived(line);

                        if (token.IsCancellationRequested)
                        {
                            if (!Process.HasExited)
                                Process.Kill();
                            Process.Close();
                            token.ThrowIfCancellationRequested();
                        }

                        line = outputStream.ReadLine();
                    }
                    outputProcessor.LineReceived(null);
                }

                if (Process.StartInfo.CreateNoWindow)
                {
                    while (!WaitForExit(500))
                    {
                        if (token.IsCancellationRequested)
                        {
                            Process.Kill();
                            Process.Close();
                        }
                        token.ThrowIfCancellationRequested();
                    }

                    if (Process.ExitCode != 0 && errors.Count > 0)
                    {
                        thrownException = new ProcessException(Process.ExitCode, string.Join(Environment.NewLine, errors.ToArray()));
                    }
                }
            }
            catch (Exception ex)
            {
                var errorCode = -42;
                if (ex is Win32Exception)
                    errorCode = ((Win32Exception)ex).NativeErrorCode;

                StringBuilder sb = new StringBuilder();
                if (Process.StartInfo.Arguments.Contains("-credential"))
                    sb.AppendLine($"'{Process.StartInfo.FileName} {taskName}'");
                else
                    sb.AppendLine($"'{Process.StartInfo.FileName} {Process.StartInfo.Arguments}'");
                if (errorCode == 2)
                    sb.AppendLine("The system cannot find the file specified.");
                foreach (string env in Process.StartInfo.EnvironmentVariables.Keys)
                {
                    sb.AppendFormat("{0}:{1}", env, Process.StartInfo.EnvironmentVariables[env]);
                    sb.AppendLine();
                }
                thrownException = new ProcessException(errorCode, sb.ToString(), ex);
            }

            if (thrownException != null || errors.Count > 0)
                onError?.Invoke(thrownException, string.Join(Environment.NewLine, errors.ToArray()));
            onEnd?.Invoke();
        }

        private bool WaitForExit(int milliseconds)
        {
            //Logger.Debug("WaitForExit - time: {0}ms", milliseconds);

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

        private Exception thrownException = null;

        public ProcessTask(CancellationToken token, IOutputProcessor<T> outputProcessor = null)
            : base(token)
        {
            this.outputProcessor = outputProcessor;
        }

        /// <summary>
        /// Process that calls git with the passed arguments
        /// </summary>
        /// <param name="token"></param>
        /// <param name="arguments"></param>
        /// <param name="outputProcessor"></param>
        public ProcessTask(CancellationToken token, string arguments, IOutputProcessor<T> outputProcessor = null)
            : base(token)
        {
            Guard.ArgumentNotNull(token, nameof(token));

            this.outputProcessor = outputProcessor;
            ProcessArguments = arguments;
        }

        public virtual void Configure(ProcessStartInfo psi)
        {
            Guard.ArgumentNotNull(psi, "psi");

            ConfigureOutputProcessor();

            Guard.NotNull(this, outputProcessor, nameof(outputProcessor));
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
            Name = ProcessArguments;
        }

        public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor)
        {
            outputProcessor = processor ?? outputProcessor;
            ConfigureOutputProcessor();

            Guard.NotNull(this, outputProcessor, nameof(outputProcessor));

            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
            Name = ProcessArguments;
        }

        public void Configure(Process existingProcess)
        {
            Guard.ArgumentNotNull(existingProcess, "existingProcess");

            ConfigureOutputProcessor();

            Guard.NotNull(this, outputProcessor, nameof(outputProcessor));

            Process = existingProcess;
            ProcessName = existingProcess.StartInfo.FileName;
            Name = ProcessArguments;
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

        protected override T RunWithReturn(bool success)
        {
            var result = base.RunWithReturn(success);

            wrapper = new ProcessWrapper(Name, Process, outputProcessor,
                RaiseOnStart,
                () =>
                {
                    try
                    {
                        if (outputProcessor != null)
                            result = outputProcessor.Result;

                        if (typeof(T) == typeof(string) && result == null && !Process.StartInfo.CreateNoWindow)
                            result = (T)(object)"Process running";

                        if (!String.IsNullOrEmpty(Errors))
                            OnErrorData?.Invoke(Errors);
                    }
                    catch (Exception ex)
                    {
                        if (thrownException == null)
                            thrownException = new ProcessException(ex.Message, ex);
                        else
                            thrownException = new ProcessException(thrownException.GetExceptionMessage(), ex);
                    }

                    try
                    {
                        if (thrownException != null && !RaiseFaultHandlers(thrownException))
                            throw thrownException;
                    }
                    finally
                    {
                        RaiseOnEnd(result);
                    }
                },
                (ex, error) =>
                {
                    thrownException = ex;
                    Errors = error;
                },
                Token);

            wrapper.Run();

            return result;
        }

        public override string ToString()
        {
            return $"{Task?.Id ?? -1} {Name} {GetType()} {ProcessName} {ProcessArguments}";
        }

        public Process Process { get; set; }
        public int ProcessId { get { return Process.Id; } }
        public override bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Process.ExitCode == 0; } }
        public StreamWriter StandardInput { get { return wrapper?.Input; } }
        public virtual string ProcessName { get; protected set; }
        public virtual string ProcessArguments { get; }
    }

    class ProcessTaskWithListOutput<T> : DataTaskBase<T, List<T>>, IProcessTask<T, List<T>>
    {
        private IOutputProcessor<T, List<T>> outputProcessor;
        private Exception thrownException = null;
        private ProcessWrapper wrapper;

        public event Action<string> OnErrorData;
        public event Action<IProcess> OnStartProcess;
        public event Action<IProcess> OnEndProcess;

        public ProcessTaskWithListOutput(CancellationToken token)
            : base(token)
        {}

        public ProcessTaskWithListOutput(CancellationToken token, IOutputProcessor<T, List<T>> outputProcessor = null)
            : base(token)
        {
            this.outputProcessor = outputProcessor;
        }

        public virtual void Configure(ProcessStartInfo psi)
        {
            Guard.ArgumentNotNull(psi, "psi");

            ConfigureOutputProcessor();

            Guard.NotNull(this, outputProcessor, nameof(outputProcessor));

            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
        }

        public void Configure(Process existingProcess)
        {
            Guard.ArgumentNotNull(existingProcess, "existingProcess");

            ConfigureOutputProcessor();
            Guard.NotNull(this, outputProcessor, nameof(outputProcessor));
            Process = existingProcess;
            ProcessName = existingProcess.StartInfo.FileName;
        }

        public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T, List<T>> processor)
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
            var result = base.RunWithReturn(success);

            wrapper = new ProcessWrapper(Name, Process, outputProcessor,
                RaiseOnStart,
                () =>
                {
                    try
                    {
                        if (outputProcessor != null)
                            result = outputProcessor.Result;
                        if (result == null)
                            result = new List<T>();

                        if (!String.IsNullOrEmpty(Errors))
                            OnErrorData?.Invoke(Errors);
                    }
                    catch (Exception ex)
                    {
                        if (thrownException == null)
                            thrownException = new ProcessException(ex.Message, ex);
                        else
                            thrownException = new ProcessException(thrownException.GetExceptionMessage(), ex);
                    }

                    try
                    {
                        if (thrownException != null && !RaiseFaultHandlers(thrownException))
                            throw thrownException;
                    }
                    finally
                    {
                        RaiseOnEnd(result);
                    }
                },
                (ex, error) =>
                {
                    thrownException = ex;
                    Errors = error;
                },
                Token);
            wrapper.Run();

            return result;
        }

        public override string ToString()
        {
            return $"{Task?.Id ?? -1} {Name} {GetType()} {ProcessName} {ProcessArguments}";
        }

        public Process Process { get; set; }
        public int ProcessId { get { return Process.Id; } }
        public override bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Process.ExitCode == 0; } }
        public StreamWriter StandardInput { get { return wrapper?.Input; } }
        public virtual string ProcessName { get; protected set; }
        public virtual string ProcessArguments { get; }
    }

    class FirstNonNullLineProcessTask : ProcessTask<string>
    {
        private readonly NPath fullPathToExecutable;
        private readonly string arguments;

        public FirstNonNullLineProcessTask(CancellationToken token, NPath fullPathToExecutable, string arguments)
            : base(token, new FirstNonNullLineOutputProcessor())
        {
            this.fullPathToExecutable = fullPathToExecutable;
            this.arguments = arguments;
        }

        public override string ProcessName => fullPathToExecutable.FileName;
        public override string ProcessArguments => arguments;
    }

    class SimpleProcessTask : ProcessTask<string>
    {
        private readonly NPath? fullPathToExecutable;
        private readonly string arguments;

        public SimpleProcessTask(CancellationToken token, NPath fullPathToExecutable, string arguments, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            this.fullPathToExecutable = fullPathToExecutable;
            this.arguments = arguments;
        }

        public SimpleProcessTask(CancellationToken token, string arguments, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            this.arguments = arguments;
        }

        public override string ProcessName => fullPathToExecutable?.FileName;
        public override string ProcessArguments => arguments;
    }

    class SimpleListProcessTask : ProcessTaskWithListOutput<string>
    {
        private readonly NPath fullPathToExecutable;
        private readonly string arguments;

        public SimpleListProcessTask(CancellationToken token, NPath fullPathToExecutable, string arguments, IOutputProcessor<string, List<string>> processor = null)
            : base(token, processor ?? new SimpleListOutputProcessor())
        {
            this.fullPathToExecutable = fullPathToExecutable;
            this.arguments = arguments;
        }
        
        public override string ProcessName => fullPathToExecutable;
        public override string ProcessArguments => arguments;
    }
}