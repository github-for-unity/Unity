using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Unity.Logging;

namespace GitHub.Unity
{
    class GitListBranchesTask : GitTask
    {
        private const string LocalArguments = "branch -vv";
        private const string RemoteArguments = "branch -r";
        private const string UnmatchedLineError = "Unable to match the line '{0}'";
        private List<GitBranch> branches = new List<GitBranch>();
        private Mode mode;
        private Action<IEnumerable<GitBranch>> callback;
        private readonly BranchListOutputProcessor processor = new BranchListOutputProcessor();

        private GitListBranchesTask(Mode mode, Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
            : base(null, onFailure)
        {
            this.mode = mode;
            this.callback = onSuccess;
        }

        public static void ScheduleLocal(Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
        {
            Schedule(Mode.Local, onSuccess, onFailure);
        }

        public static void ScheduleRemote(Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
        {
            Schedule(Mode.Remote, onSuccess, onFailure);
        }

        private static void Schedule(Mode mode, Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitListBranchesTask(mode, onSuccess, onFailure));
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnBranch += AddBranch;
            return new ProcessOutputManager(process, processor);
        }

        protected override void OnProcessOutputUpdate()
        {
            Logger.Debug("Done");
            Tasks.ScheduleMainThread(() => DeliverResult());
        }

        private void AddBranch(GitBranch branch)
        {
            Logger.Debug("AddBranch " + branch);
            branches.Add(branch);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(branches);
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
            get { return "git list branch"; }
        }

        protected override string ProcessArguments
        {
            get { return mode == Mode.Local ? LocalArguments : RemoteArguments; }
        }

        private enum Mode
        {
            Local,
            Remote
        }
    }
}
