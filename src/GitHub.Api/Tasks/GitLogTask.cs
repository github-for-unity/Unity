using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitLogTask : GitTask
    {
        private readonly ITaskResultDispatcher<IEnumerable<GitLogEntry>> resultDispatcher;
        private List<GitLogEntry> gitLogEntries;
        private LogEntryOutputProcessor processor;

        public GitLogTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<IEnumerable<GitLogEntry>> resultDispatcher,
            IGitObjectFactory gitObjectFactory)
            : base(environment, processManager)
        {
            Guard.ArgumentNotNull(gitObjectFactory, "gitObjectFactory");

            this.resultDispatcher = resultDispatcher;

            gitLogEntries = new List<GitLogEntry>();
            processor = new LogEntryOutputProcessor(gitObjectFactory);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnLogEntry += AddLogEntry;
            return new ProcessOutputManager(process, processor);
        }

        protected override void RaiseOnSuccess()
        {
            resultDispatcher.ReportSuccess(gitLogEntries);
        }

        private void AddLogEntry(GitLogEntry gitLogEntry)
        {
            gitLogEntries.Add(gitLogEntry);
        }

        public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git log"; } }

        protected override string ProcessArguments
        {
            get { return @"log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status"; }
        }
    }
}
