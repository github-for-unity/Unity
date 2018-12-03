using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitConfigUnSetTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitConfigUnSetTask(string key, GitConfigSource configSource,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--unset" :
                configSource == GitConfigSource.Local ? "--local --unset" :
                configSource == GitConfigSource.User ? "--global --unset" :
                "--system --unset";
            arguments = String.Format("config {0} {1}", source, key);
            Name = String.Format("config {0} {1}", source, key);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Writing configuration...";
    }
}