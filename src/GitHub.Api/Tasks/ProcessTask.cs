using GitHub.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class ProcessTask : BaseTask
    {
        private const int ExitMonitorSleep = 100;
        private const string ProcessKey = "process";
        private const string TypeKey = "type";

        private readonly StringWriter error = new StringWriter();
        private readonly StringWriter output = new StringWriter();
        private readonly IProcessManager processManager;
        private readonly IEnvironment environment;
        private readonly ITaskResultDispatcher resultDispatcher;

        public Action<IProcess> OnCreateProcess;

        private Action onFailure;
        private Action<string> onSuccess;

        private IProcess process;
        private CancellationToken cancellationToken;
        private bool finishedRaised;
        private string arguments = null;
        private string processName = null;

        public ProcessTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            string name,
            Action<string> onSuccess = null, Action onFailure = null)
        {
            this.processName = name;
            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
            this.environment = environment;
            this.processManager = processManager;
            this.resultDispatcher = resultDispatcher;
        }

        public ProcessTask(IEnvironment environment, IProcessManager processManager,
                string name,
                Action<string> onSuccess = null, Action onFailure = null)
            : this (environment, processManager, null, name, onSuccess, onFailure)
        {
        }

        public ProcessTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher = null,
            Action<string> onSuccess = null, Action onFailure = null)
        {
            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
            this.environment = environment;
            this.processManager = processManager;
            this.resultDispatcher = resultDispatcher;
        }

        /// <summary>
        /// Try to reattach to the process. Assume that we're done if that fails.
        /// </summary>
        /// <returns></returns>
        public static ProcessTask Parse(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            IDictionary<string, object> data)
        {
            IProcess resumedProcess = null;

            try
            {
                resumedProcess = processManager.Reconnect((int)(Int64)data[ProcessKey]);
            }
            catch (Exception)
            {
                resumedProcess = null;
            }

            return new ProcessTask(environment, processManager, resultDispatcher)
            {
                process = resumedProcess,
                Done = resumedProcess == null,
                Progress = resumedProcess == null ? 1f : 0f
            };
        }

        public override void Run(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            Logger.Debug("RunTask Label:\"{0}\" Type:{1}", Label, process == null ? "start" : "reconnect");

            Done = false;
            Progress = 0.0f;

            OnBegin?.Invoke(this);

            var firstTime = process == null;

            // Only start the process if we haven't already reconnected to an existing instance
            if (firstTime)
            {
                process = processManager.Configure(ProcessName, ProcessArguments, environment.RepositoryPath);
            }

            process.OnExit += p =>
            {
                //Logger.Debug("Exit");
                Finished();
            };

            ProcessOutputManager outputManager;
            try
            {
                outputManager = HookupOutput(process);
                OnCreateProcess?.Invoke(process);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            if (firstTime)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        RaiseOnEnd();
                        return;
                    }
                    process.Run();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            if (process.HasExited)
            {
                try
                {
                    Finished();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        public override Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;

            Logger.Trace("RunTaskAsync Label:\"{0}\" Type:{1}", Label, process == null ? "start" : "reconnect");

            Done = false;
            Progress = 0.0f;

            OnBegin?.Invoke(this);

            var firstTime = process == null;

            // Only start the process if we haven't already reconnected to an existing instance
            if (firstTime)
            {
                process = processManager.Configure(ProcessName, ProcessArguments, environment.RepositoryPath);
            }

            //process.OnExit += p =>
            //{
            //    //Logger.Debug("Exit");
            //    //Finished();
            //};

            ProcessOutputManager outputManager;
            try
            {
                outputManager = HookupOutput(process);
                OnCreateProcess?.Invoke(process);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            if (firstTime)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        RaiseOnEnd();
                        return TaskEx.FromResult(false);
                    }
                    process.Run();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            var processDone = false;
            do
            {
                processDone = process.WaitForExit(100);
                if (this.cancellationToken.IsCancellationRequested)
                {
                    Abort();
                    return TaskEx.FromResult(false);
                }
            }
            while (!processDone);

            if (processDone)
            {
                try
                {
                    Finished();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            return TaskEx.FromResult(process.Successful);
        }

        private void Finished()
        {
            if (finishedRaised)
            {
                return;
            }

            finishedRaised = true;
            //Logger.Debug("Finished");

            Progress = 1.0f;

            OnOutputComplete(OutputBuffer.ToString().Trim(), ErrorBuffer.ToString().Trim());

            RaiseOnEnd();
        }

        public override void Abort()
        {
            Logger.Trace("Aborting");

            try
            {
                process.Kill();
            }
            catch (Exception)
            {}

            RaiseOnEnd();
        }

        public override void Disconnect()
        {
            Logger.Trace("Disconnect");

            process = null;
        }

        public void SetArguments(string args)
        {
            arguments = args;
        }

        public override void WriteCache(TextWriter cache)
        {
            Logger.Trace("WritingCache");

            cache.WriteLine("{");
            cache.WriteLine(String.Format("\"{0}\": \"{1}\",", TypeKey, CachedTaskType));
            cache.WriteLine(String.Format("\"{0}\": \"{1}\"", ProcessKey, process == null ? -1 : process.Id));
            cache.WriteLine("}");
        }

        protected virtual ProcessOutputManager HookupOutput(IProcess process)
        {
            var outputProcessor = new BaseOutputProcessor();
            outputProcessor.OnData += OutputBuffer.WriteLine;
            return new ProcessOutputManager(process, outputProcessor);
        }

        protected virtual void OnOutputComplete(string output, string errors)
        {
            if (errors.Length > 0)
            {
                RaiseOnFailure(errors);
            }
            else
            {
                RaiseOnSuccess(output);
            }
        }

        protected virtual void RaiseOnSuccess(string msg)
        {
            if (OnSuccess != null)
            {
                Logger.Trace("Success: \"{0}\"", msg);
                if (resultDispatcher != null)
                {
                    resultDispatcher.ReportSuccess(() => OnSuccess(msg));
                }
                else
                {
                    OnSuccess(msg);
                }
            }
        }

        protected virtual void RaiseOnFailure(string msg, FailureSeverity severity = FailureSeverity.Critical)
        {
            if (resultDispatcher != null)
            {
                resultDispatcher.ReportFailure(severity, Label, msg);
            }

            if (OnFailure != null)
            {
                Logger.Trace("Failure: \"{0}\"", msg);
                if (resultDispatcher != null)
                {
                    resultDispatcher.ReportFailure(OnFailure);
                }
                else
                {
                    OnFailure?.Invoke();
                }
            }
        }

        private void RaiseOnEnd()
        {
            //Logger.Trace("RaiseOnEnd");
            OnEnd?.Invoke(this);
            Done = true;
        }

        bool disposed = false;
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    ErrorBuffer.Dispose();
                    OutputBuffer.Dispose();
                }
            }
        }

        public override bool Blocking { get { return true; } }
        public override bool Critical { get { return true; } }
        public override bool Cached { get { return true; } }

        public override TaskQueueSetting Queued { get { return TaskQueueSetting.Queue; } }

        public override string Label { get { return "Process task"; } }

        protected virtual string ProcessName { get { return processName; } }
        protected virtual string ProcessArguments { get { return arguments; } }

        protected virtual CachedTask CachedTaskType { get { return CachedTask.ProcessTask; } }

        protected virtual StringWriter OutputBuffer { get { return output; } }
        protected virtual StringWriter ErrorBuffer { get { return error; } }

        protected virtual Action<string> OnSuccess { get { return onSuccess; } }
        protected virtual Action OnFailure { get { return onFailure; } }

        protected IProcessManager ProcessManager => processManager;
        protected IEnvironment Environment => environment;
        protected ITaskResultDispatcher TaskResultDispatcher => resultDispatcher;
    }
}
