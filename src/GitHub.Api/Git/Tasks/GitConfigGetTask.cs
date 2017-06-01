using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitConfigGetAllTask : ProcessTaskWithListOutput<string>
    {
        private readonly string arguments;

        public GitConfigGetAllTask(string key, GitConfigSource configSource,
            CancellationToken token, BaseOutputListProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new SimpleListOutputProcessor(), dependsOn)
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = String.Format("config {0} {1}", source, key);
        }

        public override string Name { get { return "git config get"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }

    class GitConfigGetTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitConfigGetTask(string key, GitConfigSource configSource,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new FirstNonNullLineOutputProcessor(), dependsOn)
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = String.Format("config {0} {1}", source, key);
        }

        public override string Name { get { return "git config get"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}
