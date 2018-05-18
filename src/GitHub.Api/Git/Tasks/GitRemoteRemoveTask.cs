using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitRemoteRemoveTask : ProcessTask<string>
    {
        private const string TaskName = "git remote rm";
        private readonly string arguments;

        public GitRemoteRemoveTask(string remote,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Name = TaskName;
            arguments = String.Format("remote rm {0}", remote);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Deleting remote...";
    }
}
