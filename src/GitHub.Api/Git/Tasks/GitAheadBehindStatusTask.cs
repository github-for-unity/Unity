using System.Threading;

namespace GitHub.Unity
{
    class GitAheadBehindStatusTask : ProcessTask<GitAheadBehindStatus>
    {
        private const string TaskName = "git rev-list";
        private readonly string arguments;

        public GitAheadBehindStatusTask(string gitRef, string otherRef,
            CancellationToken token, IOutputProcessor<GitAheadBehindStatus> processor = null)
            : base(token, processor ?? new GitAheadBehindStatusOutputProcessor())
        {
            Name = TaskName;
            arguments = $"rev-list --left-right --count {gitRef}...{otherRef}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Querying status...";
    }
}