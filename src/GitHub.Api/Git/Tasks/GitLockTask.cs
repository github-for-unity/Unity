using System;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitLockTask : ProcessTask<string>
    {
        private const string TaskName = "git lfs lock";
        private readonly string arguments;

        public GitLockTask(string path,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Name = TaskName;
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            arguments = String.Format("lock \"{0}\"", path);
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Locking file...";
    }
}
