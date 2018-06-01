using System;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    class GitPullTask : ProcessTask<string>
    {
        private const string TaskName = "git pull";
        private readonly string arguments;

        public GitPullTask(string remote, string branch,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new GitNetworkOperationOutputProcessor())
        {
            Name = TaskName;
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("pull");

            if (!String.IsNullOrEmpty(remote))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(remote);
            }

            if (!String.IsNullOrEmpty(branch))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(branch);
            }

            arguments = stringBuilder.ToString();
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Pulling...";
    }
}
