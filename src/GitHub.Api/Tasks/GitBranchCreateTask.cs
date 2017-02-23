using GitHub.Unity;
using System;
using System.IO;

namespace GitHub.Unity
{
    class GitBranchCreateTask : GitTask
    {
        private readonly string arguments;

        public GitBranchCreateTask(IEnvironment environment, IProcessManager processManager,
                ITaskResultDispatcher<String> resultDispatcher,
                string newBranch, string baseBranch)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(newBranch, "newBranch");
            Guard.ArgumentNotNullOrWhiteSpace(baseBranch, "baseBranch");

            arguments = String.Format("branch {0} {1}", newBranch, baseBranch);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git branch"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
