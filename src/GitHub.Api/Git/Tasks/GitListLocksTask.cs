using System.Threading;

namespace GitHub.Unity
{
    class GitListLocksTask : ProcessTaskWithListOutput<GitLock>
    {
        private const string TaskName = "git lfs locks";
        private readonly string args;

        public GitListLocksTask(bool local,
            CancellationToken token, BaseOutputListProcessor<GitLock> processor = null)
            : base(token, processor ?? new LocksOutputProcessor())
        {
            Name = TaskName;
            args = "locks --json";
            if (local)
            {
                args += " --local";
            }
        }

        public override string ProcessArguments => args;
        public override string Message { get; set; } = "Reading locks...";
    }
}
