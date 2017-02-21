using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitListLocksTask : GitTask
    {
        private readonly ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher;
        private readonly LockOutputProcessor processor;

        private List<GitLock> gitLocks = new List<GitLock>();

        private GitListLocksTask(
            IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher,
            IGitObjectFactory gitObjectFactory)
            : base(environment, processManager)
        {
            Guard.ArgumentNotNull(gitObjectFactory, "gitStatusEntryFactory");

            this.resultDispatcher = resultDispatcher;
            processor = new LockOutputProcessor(gitObjectFactory);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnGitLock += AddLock;
            return new ProcessOutputManager(process, processor);
        }

        protected override void RaiseOnSuccess()
        {
            resultDispatcher.ReportSuccess(gitLocks);
        }

        private void AddLock(GitLock gitLock)
        {
            Logger.Trace("AddLock " + gitLock);
            gitLocks.Add(gitLock);
        }

        public override bool Blocking { get { return false; } }
        public override TaskQueueSetting Queued { get { return TaskQueueSetting.Queue; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git lfs locks"; } }
        protected override string ProcessArguments { get { return "lfs locks"; } }
    }
}
