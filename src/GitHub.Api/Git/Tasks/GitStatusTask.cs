using System.Threading;

namespace GitHub.Unity
{
    class GitStatusTask : ProcessTask<GitStatus?>
    {
        private const string TaskName = "git status";

        public GitStatusTask(IGitObjectFactory gitObjectFactory,
            CancellationToken token, IOutputProcessor<GitStatus?> processor = null)
            : base(token, processor ?? new StatusOutputProcessor(gitObjectFactory))
        {
            Name = TaskName;
        }

        public override string ProcessArguments
        {
            get { return "status -b -u --ignored --porcelain"; }
        }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
