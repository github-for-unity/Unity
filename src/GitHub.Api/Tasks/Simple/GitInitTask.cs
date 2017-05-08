using System.Threading;

namespace GitHub.Unity
{
    class GitInitTask : ProcessTask<string>
    {
        public GitInitTask(CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        { }

        public override string Name { get { return "git init"; } }
        public override string ProcessArguments { get { return "init"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
