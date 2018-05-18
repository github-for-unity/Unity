using System.Threading;

namespace GitHub.Unity
{
    class GitInitTask : ProcessTask<string>
    {
        private const string TaskName = "git init";

        public GitInitTask(CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "init"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Initializing...";
    }
}
