using System;
using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitConfigListTask : ProcessTaskWithListOutput<KeyValuePair<string, string>>
    {
        private const string TaskName = "git config list";
        private readonly string arguments;

        public GitConfigListTask(GitConfigSource configSource, CancellationToken token, BaseOutputListProcessor<KeyValuePair<string, string>> processor = null)
            : base(token, processor ?? new ConfigOutputProcessor())
        {
            Name = TaskName;
            var source = "";
            if (configSource != GitConfigSource.NonSpecified)
            {
                source = "--";
                source += configSource == GitConfigSource.Local
                    ? "local"
                    : (configSource == GitConfigSource.User
                        ? "system"
                        : "global");
            }
            arguments = String.Format("config {0} -l", source);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Reading configuration...";
    }
}
