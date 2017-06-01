using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitPushTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitPushTask(CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
            arguments = "push";
        }

        public GitPushTask(string remote, string branch, bool setUpstream,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            arguments = String.Format("push {0} {1} {2}:{2}",
                setUpstream ? "-u" : "",
                remote, branch);
        }

        public override string Name { get { return "git push"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
