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
            //TODO: Find a better place to build this object
            var gitStatusEntryFactory = new GitStatusEntryFactory(EntryPoint.Environment, EntryPoint.FileSystem, EntryPoint.GitEnvironment);

            Tasks.Add(new GitLogTask(onSuccess, onFailure, gitStatusEntryFactory));
        }

        private GitLogTask(Action<IList<GitLogEntry>> onSuccess, Action onFailure, IGitStatusEntryFactory gitStatusEntryFactory)
            : base(null, onFailure)
        {
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
            Tasks.ScheduleMainThread(() => DeliverResult());
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(gitLogEntries);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnLogEntry += gitLogEntry => {
                gitLogEntries.Add(gitLogEntry);
            };
            return new ProcessOutputManager(process, processor);
        }
    }
}