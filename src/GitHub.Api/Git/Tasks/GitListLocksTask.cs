using System.Threading;

namespace GitHub.Unity
{
    class GitListLocksTask : ProcessTaskWithListOutput<GitLock>
    {
        private const string TaskName = "git lfs locks";
        private readonly string args;

        public GitListLocksTask(IGitObjectFactory gitObjectFactory, bool local,
            CancellationToken token, BaseOutputListProcessor<GitLock> processor = null)
            : base(token, processor ?? new LockOutputProcessor(gitObjectFactory))
        {
            Name = TaskName;
            args = "lfs locks";
            if (local)
            {
                args += " --local";
            }
        }

        public override string ProcessArguments => args;
    }
}
