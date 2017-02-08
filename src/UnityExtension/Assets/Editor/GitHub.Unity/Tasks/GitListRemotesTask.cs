using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitListRemotesTask : GitTask
    {
        private Action<IList<GitRemote>> callback;
        private RemoteListOutputProcessor processor;

        private List<GitRemote> remotes = new List<GitRemote>();

        private GitListRemotesTask(Action<IList<GitRemote>> onSuccess, Action onFailure = null)
            : base(null, onFailure)
        {
            processor = new RemoteListOutputProcessor();
            callback = onSuccess;
        }

        public static void Schedule(Action<IList<GitRemote>> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitListRemotesTask(onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            Logger.Debug("Done");
            Tasks.ScheduleMainThread(DeliverResult);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnRemote += AddRemote;
            return new ProcessOutputManager(process, processor);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(remotes);
        }

        private void AddRemote(GitRemote remote)
        {
            remotes.Add(remote);
        }

        public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git remote"; } }
        protected override string ProcessArguments { get { return "remote -v"; } }
    }
}
