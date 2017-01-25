using GitHub.Unity.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GitHub.Unity
{
    class ProcessTask : ITask, IDisposable
    {
        private const int ExitMonitorSleep = 100;

        private readonly StringWriter error = new StringWriter();
        private readonly StringWriter output = new StringWriter();

        private Action onFailure;
        private Action<string> onSuccess;

        private IProcess process;

        protected ProcessTask(Action<string> onSuccess = null, Action onFailure = null)
        {
            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
        }

        [MenuItem("Assets/GitHub/Process Test")]
        public static void Test()
        {
            EditorApplication.delayCall += () => Tasks.Add(new ProcessTask());
        }

        /// <summary>
        /// Try to reattach to the process. Assume that we're done if that fails.
        /// </summary>
        /// <returns></returns>
        public static ProcessTask Parse(IDictionary<string, object> data)
        {
            IProcess resumedProcess;

            try
            {
                resumedProcess = ProcessManager.Instance.Reconnect((int)(Int64)data[Tasks.ProcessKey]);
            }
            catch (Exception)
            {
                resumedProcess = null;
            }

            return new ProcessTask {
                process = resumedProcess,
                Done = resumedProcess == null,
                Progress = resumedProcess == null ? 1f : 0f
            };
        }

        public virtual void Run()
        {
            Debug.LogFormat("{0} {1} ({2})", Label, process == null ? "start" : "reconnect", System.Threading.Thread.CurrentThread.ManagedThreadId);

            Done = false;
            Progress = 0.0f;

            OnBegin.SafeInvoke(this);

            var firstTime = process == null;

            // Only start the process if we haven't already reconnected to an existing instance
            if (firstTime)
            {
                process = ProcessManager.Instance.Configure(ProcessName, ProcessArguments, Utility.GitRoot);
            }

            process.OnExit += p =>
            {
                UnityEngine.Debug.Log("OnExit (" + System.Threading.Thread.CurrentThread.ManagedThreadId + ")");
                Finished();
            };

            var outputManager = HookupOutput(process);

            if (firstTime)
            {
                process.Run();
            }

            if (process.HasExited)
            {
                Finished();
            }

            // NOTE: WaitForExit is too low level here. Won't be properly interrupted by thread abort.
            //do
            //{
            //    //process.WaitForExit(ExitMonitorSleep);
            //    // Wait a bit
            //    //Thread.Sleep(ExitMonitorSleep);

            //    // Read all available process output
            //    //var updated = false;

            //    //while (!process.StandardOutput.EndOfStream)
            //    //{
            //    //    var read = (char)process.StandardOutput.Read();
            //    //    OutputBuffer.Write(read);
            //    //    updated = true;
            //    //}

            //    //while (!process.StandardError.EndOfStream)
            //    //{
            //    //    var read = (char)process.StandardError.Read();
            //    //    ErrorBuffer.Write(read);
            //    //    updated = true;
            //    //}

            //    //// Notify if anything was read
            //    //if (updated)
            //    //{
            //    //    OnProcessOutputUpdate();
            //    //}
            //} while (!process.HasExited);

        }

        private void Finished()
        {
            Progress = 1.0f;
            Done = true;

            OnProcessOutputUpdate();

            Debug.LogFormat("{0} end", Label);

            OnEnd.SafeInvoke(this);
        }

        public void Abort()
        {
            Debug.LogFormat("Aborting {0}", Label);

            try
            {
                process.Kill();
            }
            catch (Exception)
            {}

            Done = true;

            OnEnd.SafeInvoke(this);
        }

        public void Disconnect()
        {
            Debug.LogFormat("Disconnect {0}", Label);

            process = null;
        }

        public void Reconnect()
        {}

        public void WriteCache(TextWriter cache)
        {
            Debug.LogFormat("Writing cache for {0}", Label);

            cache.WriteLine("{");
            cache.WriteLine(String.Format("\"{0}\": \"{1}\",", Tasks.TypeKey, CachedTaskType));
            cache.WriteLine(String.Format("\"{0}\": \"{1}\"", Tasks.ProcessKey, process == null ? -1 : process.Id));
            cache.WriteLine("}");
        }

        protected virtual ProcessOutputManager HookupOutput(IProcess process)
        {
            var outputProcessor = new BaseOutputProcessor();
            outputProcessor.OnData += OutputBuffer.WriteLine;
            return new ProcessOutputManager(process, outputProcessor);
        }

        protected virtual void OnProcessOutputUpdate()
        {
            if (!Done)
            {
                return;
            }

            var buffer = ErrorBuffer.GetStringBuilder();
            if (buffer.Length > 0)
            {
                ReportFailure(buffer.ToString());
            }
            else
            {
                ReportSuccess(OutputBuffer.ToString().Trim());
            }
        }

        protected void ReportSuccess(string msg)
        {
            if (OnSuccess != null)
            {
                Tasks.ScheduleMainThread(() => OnSuccess(msg));
            }
        }

        protected void ReportFailure(string msg)
        {
            Tasks.ReportFailure(FailureSeverity.Critical, this, msg);

            if (OnFailure != null)
            {
                Tasks.ScheduleMainThread(() => OnFailure());
            }
        }

        bool disposed = false;
        public virtual void Dispose(bool disposing)
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual float Progress { get; protected set; }
        public virtual bool Done { get; protected set; }

        public virtual bool Blocking { get { return true; } }
        public virtual bool Critical { get { return true; } }
        public virtual bool Cached { get { return true; } }

        public virtual TaskQueueSetting Queued { get { return TaskQueueSetting.Queue; } }

        public virtual Action<ITask> OnBegin { get; set; }
        public virtual Action<ITask> OnEnd { get; set; }
        public virtual string Label { get { return "Process task"; } }

        protected virtual string ProcessName { get { return "sleep"; } }
        protected virtual string ProcessArguments { get { return "20"; } }

        protected virtual CachedTask CachedTaskType { get { return CachedTask.ProcessTask; } }

        protected virtual StringWriter OutputBuffer { get { return output; } }
        protected virtual StringWriter ErrorBuffer { get { return error; } }

        protected virtual Action<string> OnSuccess { get { return onSuccess; } }
        protected virtual Action OnFailure { get { return onFailure; } }
    }
}
