using System;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitBranchCreateTask : ProcessTask<string>
    {
        private const string TaskName = "git branch";
        private readonly string arguments;

        public GitBranchCreateTask(string newBranch, string baseBranch,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(newBranch, "newBranch");
            Guard.ArgumentNotNullOrWhiteSpace(baseBranch, "baseBranch");

            Name = TaskName;
            arguments = String.Format("branch {0} {1}", newBranch, baseBranch);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Creating branch...";
    }
}
