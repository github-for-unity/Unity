using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitRemoteChangeTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitRemoteChangeTask(string remote, string url,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            arguments = String.Format("remote set-url {0} {1}", remote, url);
        }

        public override string Name { get { return "git remote set-url"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}