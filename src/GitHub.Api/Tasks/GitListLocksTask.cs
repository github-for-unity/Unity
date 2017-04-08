using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitListLocksTask : GitTask, ITask<IEnumerable<GitLock>>
    {
        private readonly ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher;
        private readonly LockOutputProcessor processor;
        private readonly string args;

        public GitListLocksTask(
            IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher,
            IGitObjectFactory gitObjectFactory, bool local = false)
            : base(environment, processManager)
        {
            Guard.ArgumentNotNull(gitObjectFactory, "gitStatusEntryFactory");

            this.resultDispatcher = resultDispatcher;
            processor = new LockOutputProcessor(gitObjectFactory);
            args = "lfs locks";
            if (local)
            {
                args += " --local";
            }
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnGitLock += AddLock;
            return new ProcessOutputManager(process, processor);
        }

        protected override void RaiseOnSuccess()
        {
            resultDispatcher.ReportSuccess(TaskResult);
        }

        private void AddLock(GitLock gitLock)
        {
            (TaskResult as List<GitLock>).Add(gitLock);
        }

        public override bool Blocking { get { return false; } }
        public override TaskQueueSetting Queued { get { return TaskQueueSetting.Queue; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git lfs locks"; } }
        protected override string ProcessArguments => args;
        public IEnumerable<GitLock> TaskResult
        {
            get
            {
                if (Result == null)
                {
                    Result = new List<GitLock>();
                }
                return (List<GitLock>)Result;
            }
        }

    }
}
