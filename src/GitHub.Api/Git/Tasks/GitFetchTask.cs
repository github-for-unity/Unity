using System;
using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitFetchTask : ProcessTask<string>
    {
        private const string TaskName = "git fetch";
        private readonly string arguments;

        public GitFetchTask(string remote,
            CancellationToken token, bool prune = true, bool tags = true, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new GitNetworkOperationOutputProcessor())
        {
            Name = TaskName;
            var args = new List<string> { "fetch" };
            
            if (prune)
            {
                args.Add("--prune");
            }

            if (tags)
            {
                args.Add("--tags");
            }

            if (!String.IsNullOrEmpty(remote))
            {
                args.Add(remote);
            }

            arguments = args.Join(" ");
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Fetching...";
    }
}
