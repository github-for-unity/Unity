using Rackspace.Threading;

namespace GitHub.Unity
{
    class ProgressReport
    {
        public Progress<float> Percentage = new Progress<float>();
        public Progress<long> Remaining = new Progress<long>();
    }
}