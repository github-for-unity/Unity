using System.Threading.Tasks;

namespace GitHub.Unity
{
    public class UsageTrackerDispatcher : IUsageTracker
    {
        private readonly IUsageTracker usageTracker;

        public UsageTrackerDispatcher(IUsageTracker usageTracker)
        {
            this.usageTracker = usageTracker;
        }
    }
}
