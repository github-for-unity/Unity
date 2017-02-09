using System;

namespace GitHub.Unity
{
    class GitStatusTask : GitTask
    {
        private readonly StatusOutputProcessor processor;
        private Action<GitStatus> callback;
        private GitStatus gitStatus;

        private GitStatusTask(IGitStatusEntryFactory gitStatusEntryFactory, Action<GitStatus> onSuccess, Action onFailure = null)
            : base(null, onFailure)
        {
            callback = onSuccess;
            processor = new StatusOutputProcessor(gitStatusEntryFactory);
        }

        public static void Schedule(Action<GitStatus> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitStatusTask(EntryPoint.GitStatusEntryFactory, onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            Logger.Debug("Done");
            Tasks.ScheduleMainThread(() => DeliverResult());
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnStatus += status => { gitStatus = status; };
            return new ProcessOutputManager(process, processor);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(gitStatus);
        }

        public override bool Blocking { get { return false; } }
        public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git status"; } }

        protected override string ProcessArguments
        {
            get { return "status -b -u --porcelain"; }
        }
    }
}
