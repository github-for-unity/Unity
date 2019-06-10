using System;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitRemoteAddTask : ProcessTask<string>
    {
        private const string TaskName = "git remote add";
        private readonly string arguments;

        public GitRemoteAddTask(string remote, string url,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            Name = TaskName;
            arguments = String.Format("remote add {0} {1}", remote, url);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Adding remote...";
    }
}
