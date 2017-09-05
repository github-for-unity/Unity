using System.Threading;

namespace GitHub.Unity
{
    class GitLfsVersionTask : ProcessTask<SoftwareVersion>
    {
        private const string TaskName = "git lfs version";

        public GitLfsVersionTask(CancellationToken token, IOutputProcessor<SoftwareVersion> processor = null)
            : base(token, processor ?? new LfsVersionOutputProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "lfs version"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}