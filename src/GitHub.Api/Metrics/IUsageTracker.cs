using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        bool Enabled { get; set; }
    }

    class NullUsageTracker : IUsageTracker
    {
        private static ILogging logger = Logging.GetLogger<NullUsageTracker>();

        private static Task FromVoidResult()
        {
            return TaskEx.FromResult(0).ContinueWith(task => task.Wait());
        }

        public bool Enabled { get; set; }
    }
}
