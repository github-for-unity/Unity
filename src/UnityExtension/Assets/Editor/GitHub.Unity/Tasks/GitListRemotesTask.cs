using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitListRemotesTask : GitTask
    {
        public static void Schedule(Action<IList<GitRemote>> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitListRemotesTask(onSuccess, onFailure));
        }

        private List<GitRemote> remotes = new List<GitRemote>();
        private Action<IList<GitRemote>> callback;
        private RemoteListOutputProcessor processor;

        private GitListRemotesTask(Action<IList<GitRemote>> onSuccess, Action onFailure = null)
            : base(null, onFailure)
        {
            processor = new RemoteListOutputProcessor();
            callback = onSuccess;
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
            get { return "git remote"; }
        }

        protected override string ProcessArguments
        {
            get { return "remote -v"; }
        }

        protected override void OnProcessOutputUpdate()
        {
            Logger.Debug("Done");
            Tasks.ScheduleMainThread(DeliverResult);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(remotes);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnRemote += AddRemote;
            return new ProcessOutputManager(process, processor);
        }

        private void AddRemote(GitRemote remote)
        {
            remotes.Add(remote);
        }
    }
}
