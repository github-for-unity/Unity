using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitLogTask : GitTask
    {
        private List<GitLogEntry> gitLogEntries;
        private Action<IList<GitLogEntry>> callback;
        private LogEntryOutputProcessor processor;

        public static void Schedule(Action<IList<GitLogEntry>> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitLogTask(onSuccess, onFailure, EntryPoint.GitStatusEntryFactory));
        }

        private GitLogTask(Action<IList<GitLogEntry>> onSuccess, Action onFailure,
            IGitStatusEntryFactory gitStatusEntryFactory)
            : base(null, onFailure)
        {
            gitLogEntries = new List<GitLogEntry>();
            callback = onSuccess;
            processor = new LogEntryOutputProcessor(gitStatusEntryFactory);
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override TaskQueueSetting Queued
        {
            get { return TaskQueueSetting.QueueSingle; }
        }

        public override bool Critical
        {
            get { return false; }
        }

        public override bool Cached
        {
            get { return false; }
        }

        public override string Label
        {
            get { return "git log"; }
        }

        protected override string ProcessArguments
        {
            get { return "log --name-status"; }
        }

        protected override void OnProcessOutputUpdate()
        {
            Logger.Debug("Done");
            Tasks.ScheduleMainThread(DeliverResult);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(gitLogEntries);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnLogEntry += AddLogEntry;
            return new ProcessOutputManager(process, processor);
        }

        private void AddLogEntry(GitLogEntry gitLogEntry)
        {
            gitLogEntries.Add(gitLogEntry);
        }
    }
}