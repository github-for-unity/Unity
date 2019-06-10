using System;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitSwitchBranchesTask : ProcessTask<string>
    {
        private const string TaskName = "git checkout";
        private readonly string arguments;

        public GitSwitchBranchesTask(string branch,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            Name = TaskName;
            arguments = String.Format("checkout {0}", branch);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Switching branch...";
    }
}
