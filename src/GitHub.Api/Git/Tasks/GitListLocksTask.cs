using System.Threading;

namespace GitHub.Unity
{
    class GitListLocksTask : ProcessTaskWithListOutput<GitLock>
    {
        private readonly string args;

        public GitListLocksTask(bool local,
            CancellationToken token, BaseOutputListProcessor<GitLock> processor = null)
            : base(token, processor ?? new LocksOutputProcessor())
        {
            args = "lfs locks --json";
            if (local)
            {
                args += " --local";
            }
            Name = args;
        }

        public override string ProcessArguments => args;
    }
}
