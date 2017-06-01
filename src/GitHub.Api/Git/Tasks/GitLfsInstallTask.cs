using System.Threading;

namespace GitHub.Unity
{
    class GitLfsInstallTask : ProcessTask<string>
    {
        public GitLfsInstallTask(CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        { }

        public override string Name { get { return "git lfs install"; } }
        public override string ProcessArguments { get { return "lfs install"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}