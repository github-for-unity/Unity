using System;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitPushTask : ProcessTask<string>
    {
        private const string TaskName = "git push";
        private readonly string arguments;

        public GitPushTask(CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Name = TaskName;
            arguments = "push";
        }

        public GitPushTask(string remote, string branch, bool setUpstream,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            Name = TaskName;
            arguments = String.Format("push {0} {1} {2}:{2}",
                setUpstream ? "-u" : "",
                remote, branch);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Pushing...";
    }
}
