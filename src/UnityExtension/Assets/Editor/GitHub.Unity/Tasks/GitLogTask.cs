using GitHub.Api;
using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitLogTask : GitTask
    {
        private Action<IList<GitLogEntry>> callback;
        private List<GitLogEntry> gitLogEntries;
        private LogEntryOutputProcessor processor;

        private GitLogTask(IGitStatusEntryFactory gitStatusEntryFactory,
            Action<IList<GitLogEntry>> onSuccess, Action onFailure)
            : base(null, onFailure)
        {
            Guard.ArgumentNotNull(gitStatusEntryFactory, "gitStatusEntryFactory");

            gitLogEntries = new List<GitLogEntry>();
            callback = onSuccess;
            processor = new LogEntryOutputProcessor(gitStatusEntryFactory);
        }

        public static void Schedule(Action<IList<GitLogEntry>> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitLogTask(EntryPoint.GitStatusEntryFactory, onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            Logger.Debug("Done");
            Tasks.ScheduleMainThread(DeliverResult);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnLogEntry += AddLogEntry;
            return new ProcessOutputManager(process, processor);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(gitLogEntries);
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
