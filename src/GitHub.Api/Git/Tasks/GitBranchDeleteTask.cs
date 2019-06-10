using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitBranchDeleteTask : ProcessTask<string>
    {
        private const string TaskName = "git branch -d";
        private readonly string arguments;

        public GitBranchDeleteTask(string branch, bool deleteUnmerged,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            Name = TaskName;
            arguments = !deleteUnmerged ? $"branch -d {branch}" : $"branch -D {branch}";
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Deleting branch...";
    }
}
