using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitRemoteBranchDeleteTask : ProcessTask<string>
    {
        private const string TaskName = "git push --delete";
        private readonly string arguments;

        public GitRemoteBranchDeleteTask(string remote, string branch,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            Name = TaskName;
            arguments = String.Format("push {0} --delete {1}", remote, branch);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Deleting remote branch...";
    }
}
