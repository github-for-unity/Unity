namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        bool Enabled { get; set; }
        void IncrementLaunchCount();
    }

    class NullUsageTracker : IUsageTracker
    {
        public bool Enabled { get; set; }

        public void IncrementLaunchCount(){ }
        public void SetMetricsService(IMetricsService instance)
        { }
    }
}
