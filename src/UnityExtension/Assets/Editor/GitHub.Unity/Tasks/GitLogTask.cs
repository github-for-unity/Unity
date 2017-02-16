using GitHub.Unity;
using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitLogTask : GitTask
    {
        private Action<IList<GitLogEntry>> callback;
        private List<GitLogEntry> gitLogEntries;
        private LogEntryOutputProcessor processor;

        private GitLogTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            IGitObjectFactory gitObjectFactory, Action<IList<GitLogEntry>> onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher,
                  null, onFailure)
        {
            Guard.ArgumentNotNull(gitObjectFactory, "gitObjectFactory");

            gitLogEntries = new List<GitLogEntry>();
            callback = onSuccess;
            processor = new LogEntryOutputProcessor(gitObjectFactory);
        }

        public static void Schedule(Action<IList<GitLogEntry>> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitLogTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                EntryPoint.GitObjectFactory, onSuccess, onFailure));
        }

        protected override void OnOutputComplete(string output, string errors)
        {
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
