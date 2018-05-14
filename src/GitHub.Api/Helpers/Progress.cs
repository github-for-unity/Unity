using GitHub.Logging;
using System;

namespace GitHub.Unity
{
    public interface IProgress
    {
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

    public class Progress : IProgress
    {
        private static ILogging Logger = LogHelper.GetLogger<Progress>(); 
        public ITask Task { get; internal set; }
        public float Percentage { get { return Total > 0 ? (float)(double)Value / Total : 0f; } }
        public long Value { get; internal set; }
        public long Total { get; internal set; }
        public string Message { get; internal set; }

        private long previousValue;
        public event Action<IProgress> OnProgress;

        public void UpdateProgress(IProgress progress)
        {
            Task = progress.Task;
            UpdateProgress(progress.Value, progress.Total, progress.Message);
        }

        public void UpdateProgress(long value, long total, string message = null)
        {
            Total = total;
            Value = value;
            Message = message ?? Message;
            if (Total == 0 || ((float)(double)Value / Total) - ((float)(double)previousValue / Total) > 1f / 100f)
            { // signal progress in 1% increments or if we don't know what the total is
                previousValue = Value;
                OnProgress?.Invoke(this);
            }
        }
    }
}
