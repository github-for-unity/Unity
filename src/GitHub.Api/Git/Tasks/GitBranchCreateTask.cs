using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitBranchCreateTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitBranchCreateTask(string newBranch, string baseBranch,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
            Guard.ArgumentNotNullOrWhiteSpace(newBranch, "newBranch");
            Guard.ArgumentNotNullOrWhiteSpace(baseBranch, "baseBranch");

            arguments = String.Format("branch {0} {1}", newBranch, baseBranch);
        }

        public override string Name { get { return "git branch"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
