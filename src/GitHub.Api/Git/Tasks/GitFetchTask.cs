using System;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    class GitFetchTask : ProcessTask<string>
    {
        private const string TaskName = "git fetch";
        private readonly string arguments;

        public GitFetchTask(string remote,
            CancellationToken token, bool prune = false, bool tags = false, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Name = TaskName;
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("fetch");
            
            if (prune)
            {
                stringBuilder.Append(" -p");
            }

            if (tags)
            {
                stringBuilder.Append(" --tags");
            }

            if (!String.IsNullOrEmpty(remote))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(remote);
            }

            arguments = stringBuilder.ToString();
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
