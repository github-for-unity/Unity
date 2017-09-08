using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitVersionTask : ProcessTask<Version>
    {
        private const string TaskName = "git --version";

        public GitVersionTask(CancellationToken token, IOutputProcessor<Version> processor = null)
            : base(token, processor ?? new VersionOutputProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "--version"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}