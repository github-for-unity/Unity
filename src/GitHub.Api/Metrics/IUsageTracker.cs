using System.Threading.Tasks;
using Rackspace.Threading;

namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        bool Enabled { get; set; }
        Task IncrementLaunchCount();
    }

    class NullUsageTracker : IUsageTracker
    {
        private static ILogging logger = Logging.GetLogger<NullUsageTracker>();

        public bool Enabled { get; set; }

        public Task IncrementLaunchCount()
        {
            return CompletedTask.Default;
        }
    }
}
