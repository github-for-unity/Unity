using System.Threading;

namespace GitHub.Unity
{
    class GitListLocksTask : ProcessTaskWithListOutput<GitLock>
    {
        private readonly string args;

        public GitListLocksTask(IGitObjectFactory gitObjectFactory, bool local,
            CancellationToken token, BaseOutputListProcessor<GitLock> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new LockOutputProcessor(gitObjectFactory), dependsOn)
        {
            args = "lfs locks";
            if (local)
            {
                args += " --local";
            }
        }

        public override string Name { get { return "git lfs locks"; } }
        public override string ProcessArguments => args;
    }
}
