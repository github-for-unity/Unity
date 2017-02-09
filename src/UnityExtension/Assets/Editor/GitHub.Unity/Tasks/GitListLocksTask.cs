using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitListLocksTask : GitTask
    {
        private readonly LockOutputProcessor processor = new LockOutputProcessor();
        private List<GitLock> gitLocks = new List<GitLock>();
        private Action<IEnumerable<GitLock>> callback;

        private GitListLocksTask(Action<IEnumerable<GitLock>> onSuccess, Action onFailure = null)
            : base(null, onFailure)
        {
            callback = onSuccess;
        }

        public static void ScheduleLocal(Action<IEnumerable<GitLock>> onSuccess, Action onFailure = null)
        {
            Schedule(onSuccess, onFailure);
        }

        public static void ScheduleRemote(Action<IEnumerable<GitLock>> onSuccess, Action onFailure = null)
        {
            Schedule(onSuccess, onFailure);
        }

        private static void Schedule(Action<IEnumerable<GitLock>> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitListLocksTask(onSuccess, onFailure));
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnGitLock += AddLock;
            return new ProcessOutputManager(process, processor);
        }

        protected override void OnProcessOutputUpdate()
        {
            Logger.Debug("Done");
            Tasks.ScheduleMainThread(() => DeliverResult());
        }

        private void AddLock(GitLock gitLock)
        {
            Logger.Debug("AddLock " + gitLock);
            gitLocks.Add(gitLock);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(gitLocks);
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override TaskQueueSetting Queued
        {
            get { return TaskQueueSetting.Queue; }
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
            get { return "git lfs locks"; }
        }

        protected override string ProcessArguments
        {
            get { return "lfs locks"; }
        }
    }
}
