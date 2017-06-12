using System;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    class GitFetchTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitFetchTask(string remote,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("fetch");

            if (!String.IsNullOrEmpty(remote))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(remote);
            }

            arguments = stringBuilder.ToString();
        }

        public override string Name { get { return "git fetch"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}