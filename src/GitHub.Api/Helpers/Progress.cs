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
    }

    public class Progress : IProgress
    {
        public ITask Task { get; internal set; }
        public float Percentage { get { return Total > 0 ? (float)(double)Value / Total : 0f; } }
        public long Value { get; internal set; }
        public long Total { get; internal set; }

        private long previousValue;
        private float averageSpeed = -1f;
        private float lastSpeed = 0f;
        private float smoothing = 0.005f;

        public void UpdateProgress(long value, long total)
        {
            previousValue = Value;
            Total = total;
            Value = value;
        }
    }
}
