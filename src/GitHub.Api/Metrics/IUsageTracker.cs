using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        Task IncrementLaunchCount();
    }

    class NullUsageTracker : IUsageTracker
    {
        private static ILogging logger = Logging.GetLogger<NullUsageTracker>();

        public Task IncrementLaunchCount()
        {
            logger.Trace("IncrementLaunchCount");
            return FromVoidResult();
        }

        private static Task FromVoidResult()
        {
            return TaskEx.FromResult(0).ContinueWith(task => task.Wait());
        }
    }
}
