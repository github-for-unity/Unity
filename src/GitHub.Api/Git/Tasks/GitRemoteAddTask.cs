using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitRemoteAddTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitRemoteAddTask(string remote, string url,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            arguments = String.Format("remote add {0} {1}", remote, url);
        }

        public override string Name { get { return "git remote add"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
