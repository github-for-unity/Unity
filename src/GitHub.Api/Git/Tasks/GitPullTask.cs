using System;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    class GitPullTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitPullTask(string remote, string branch,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleOutputProcessor(), dependsOn)
        {
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

        public override string Name { get { return "git pull"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
