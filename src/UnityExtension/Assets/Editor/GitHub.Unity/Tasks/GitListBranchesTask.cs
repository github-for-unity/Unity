using System;
using System.Collections.Generic;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitListBranchesTask : GitTask
    {
        private const string LocalArguments = "branch -vv";
        private const string RemoteArguments = "branch -vvr";

        private readonly List<GitBranch> branches = new List<GitBranch>();
        private readonly Mode mode;
        private readonly Action<IEnumerable<GitBranch>> callback;
        private readonly BranchListOutputProcessor processor = new BranchListOutputProcessor();

        private GitListBranchesTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                                Mode mode, Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
            : base(environment, processManager, resultDispatcher,
                  null, onFailure)
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
            Tasks.Add(new GitListBranchesTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                mode, onSuccess, onFailure));
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnBranch += AddBranch;
            return new ProcessOutputManager(process, processor);
        }

        protected override void OnOutputComplete(string output, string errors)
        {
            Tasks.ScheduleMainThread(DeliverResult);
        }

        private void AddBranch(GitBranch branch)
        {
            branches.Add(branch);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(branches);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }

        public override bool Cached { get { return false; } }

        public override string Label { get { return "git list branch"; } }

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
