using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitLockTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitLockTask(string path,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            arguments = String.Format("lfs lock \"{0}\"", path);
        }

        public override string Name { get; set; } = "git lfs lock";
        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
