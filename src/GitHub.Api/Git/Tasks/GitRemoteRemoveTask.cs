using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitRemoteRemoveTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitRemoteRemoveTask(string remote,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            arguments = String.Format("remote rm {0}", remote);
        }

        public override string Name { get { return "git remote rm"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
