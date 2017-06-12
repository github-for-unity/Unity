using System.Threading;

namespace GitHub.Unity
{
    class GitBranchDeleteTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitBranchDeleteTask(string branch, bool deleteUnmerged,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            arguments = !deleteUnmerged ? $"branch -d {branch}" : $"branch -D {branch}";
        }

        public override string Name { get { return "git branch"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
