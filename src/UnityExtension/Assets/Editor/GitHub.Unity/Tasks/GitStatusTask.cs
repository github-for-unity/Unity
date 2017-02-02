using System;

namespace GitHub.Unity
{
    class GitStatusTask : GitTask
    {
        public static void Schedule(Action<GitStatus> onSuccess, IGitStatusEntryFactory gitStatusEntryFactory,
            Action onFailure = null)
        {
            Tasks.Add(new GitStatusTask(onSuccess, gitStatusEntryFactory, onFailure));
        }

        private readonly StatusOutputProcessor processor;

        private Action<GitStatus> callback;

        private GitStatus gitStatus;

        private GitStatusTask(Action<GitStatus> onSuccess, IGitStatusEntryFactory gitStatusEntryFactory,
            Action onFailure = null)
            : base(null, onFailure)
        {
            callback = onSuccess;
            processor = new StatusOutputProcessor(gitStatusEntryFactory);
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
            get { return "git status"; }
        }

        protected override string ProcessArguments
        {
            get { return "status -b -u --porcelain"; }
        }

        protected override void OnProcessOutputUpdate()
        {
            Logger.Debug("Done");
            Tasks.ScheduleMainThread(() => DeliverResult());
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(gitStatus);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnStatus += status => {
                gitStatus = status;
            };
            return new ProcessOutputManager(process, processor);
        }
    }
}