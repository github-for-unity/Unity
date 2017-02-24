using System;

namespace GitHub.Unity
{
    class GitConfigSetTask : GitTask
    {
        private readonly ITaskResultDispatcher<string> resultDispatcher;
        private readonly string value;
        private readonly string arguments;
        private bool done = false;

        public GitConfigSetTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            string key, string value, GitConfigSource configSource)
            : base(environment, processManager, resultDispatcher)
        {
            this.resultDispatcher = resultDispatcher;
            this.value = value;
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "" :
                    configSource == GitConfigSource.Local ? "--replace-all --local" :
                        configSource == GitConfigSource.User ? "--replace-all --global" :
                            "--replace-all --system";
            arguments = String.Format("config {0} {1} \"{2}\"", source, key, value);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            var processor = new BaseOutputProcessor();
            processor.OnData += s =>
            {
                if (!done)
                {
                    done = true;
                    OutputBuffer.WriteLine(value);
                }
            };
            process.OnErrorData += ErrorBuffer.WriteLine;
            return new ProcessOutputManager(process, processor);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git config"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}