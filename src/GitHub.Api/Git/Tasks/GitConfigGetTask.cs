using System;
using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitConfigGetAllTask : ProcessTaskWithListOutput<string>
    {
        private const string TaskName = "git config get";
        private readonly string arguments;

        public GitConfigGetAllTask(string key, GitConfigSource configSource,
            CancellationToken token, BaseOutputListProcessor<string> processor = null)
            : base(token, processor ?? new SimpleListOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            Name = TaskName;
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = String.Format("config {0} {1}", source, key);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }

    class GitConfigGetTask : ProcessTask<string>
    {
        private const string TaskName = "git config get";
        private readonly string arguments;

        public GitConfigGetTask(string key, GitConfigSource configSource,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new FirstNonNullLineOutputProcessor())
        {
            Name = TaskName;
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = String.Format("config {0} {1}", source, key);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
        public override string Message { get; set; } = "Reading configuration...";
    }
}
