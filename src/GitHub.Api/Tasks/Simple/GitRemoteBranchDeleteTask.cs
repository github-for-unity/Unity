using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitRemoteBranchDeleteTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitRemoteBranchDeleteTask(string remote, string branch,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            arguments = String.Format("push {0} --delete {1}", remote, branch);
        }

        public override string Name { get { return "git push --delete"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
