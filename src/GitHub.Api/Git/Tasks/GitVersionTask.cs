using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitVersionTask : ProcessTask<TheVersion>
    {
        private const string TaskName = "git --version";

        public GitVersionTask(CancellationToken token, IOutputProcessor<TheVersion> processor = null)
            : base(token, processor ?? new VersionOutputProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "--version"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
        public override string Message { get; set; } = "Reading git version...";
    }
}
