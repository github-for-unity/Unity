using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitCountObjectsTask : ProcessTask<int>
    {
        private const string TaskName = "git count-objects";

        public GitCountObjectsTask(CancellationToken token, IOutputProcessor<int> processor = null)
            : base(token, processor ?? new GitCountObjectsProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments
        {
            get { return "count-objects"; }
        }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Counting git objects...";
    }
}
