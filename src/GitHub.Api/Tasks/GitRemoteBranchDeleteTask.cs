using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitRemoteBranchDeleteTask : GitTask
    {
        private readonly string arguments;

        public GitRemoteBranchDeleteTask(
                IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher<string> resultDispatcher,
                string remote, string branch)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            arguments = String.Format("push {0} --delete {1}", remote, branch);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git push --delete"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
