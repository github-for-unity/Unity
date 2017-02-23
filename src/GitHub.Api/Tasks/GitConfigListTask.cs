using System;

namespace GitHub.Unity
{
    class GitConfigListTask : GitTask
    {
        private readonly string arguments;
        private readonly ConfigOutputProcessor processor = new ConfigOutputProcessor();

        public GitConfigListTask(IEnvironment environment, IProcessManager processManager,
                ITaskResultDispatcher<string> resultDispatcher,
                GitConfigSource configSource)
            : base(environment, processManager, resultDispatcher)
        {
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

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnEntry += kvp =>
            {
            };
            return new ProcessOutputManager(process, processor);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git config list"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}