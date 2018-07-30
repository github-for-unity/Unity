using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    public interface IProgress
    {
        void UpdateProgress(long value, long total, string message = null);
        ITask Task { get; }
        /// <summary>
        /// From 0 to 1
        /// </summary>
        float Percentage { get; }
        long Value { get; }
        long Total { get; }
        string Message { get; }
        event Action<IProgress> OnProgress;
    }

    public class ProgressReporter
    {
        public event Action<IProgress> OnProgress;
        private Dictionary<ITask, IProgress> tasks = new Dictionary<ITask, IProgress>();
        private Progress progress = new Progress(TaskBase.Default);

        public void UpdateProgress(IProgress prog)
        {
            long total = 0;
            long value = 0;
            lock (tasks)
            {
                if (!tasks.ContainsKey(prog.Task))
                    tasks.Add(prog.Task, prog);
                else
                    tasks[prog.Task] = prog;

                total = tasks.Values.Select(x => x.Total).Sum();
                value = tasks.Values.Select(x => x.Value).Sum();

                if (prog.Percentage == 1f)
                    tasks.Remove(prog.Task);
            }
            progress.UpdateProgress(value, total, prog.Message);
            OnProgress?.Invoke(progress);
        }
    }

    public class Progress : IProgress
    {
        private static ILogging Logger = LogHelper.GetLogger<Progress>(); 
        public ITask Task { get; }
        public float Percentage { get; private set; }
        public long Value { get; private set; }
        public long Total { get; private set; }
        public string Message { get; private set; }

        private long previousValue = -1;
        public event Action<IProgress> OnProgress;

        public Progress(ITask task)
        {
            this.Task = task;
        }

        public void UpdateProgress(long value, long total, string message = null)
        {
            Total = total == 0 ? 100 : total;
            Value = value > Total ? Total : value;
            Message = String.IsNullOrEmpty(message) ? Message : message;
            float fTotal = Total;
            float fValue = Value;
            Percentage = fValue / fTotal;
            var delta = (fValue / fTotal - previousValue / fTotal) * 100f;

            if (Value != previousValue && (fValue == 0f || delta > 1f || fValue == fTotal))
            { // signal progress in 1% increments or if we don't know what the total is
                previousValue = Value;
                OnProgress?.Invoke(this);
            }
        }
    }
}
