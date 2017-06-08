using System;
using System.Threading;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitSwitchBranchesTask : ProcessTask<string>
    {
        private readonly string arguments;
        private readonly string branch;

        public GitSwitchBranchesTask(string branch,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            this.branch = branch;
            arguments = String.Format("checkout {0}", branch);
        }

        public override string Name { get { return "git checkout"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
