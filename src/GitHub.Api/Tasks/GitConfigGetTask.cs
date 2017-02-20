using System;

namespace GitHub.Unity
{
    class GitConfigGetTask : GitTask
    {
        private readonly string arguments;
        private string result;

        public GitConfigGetTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            string key, GitConfigSource configSource)
            : base(environment, processManager, resultDispatcher)
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = String.Format("config {0} {1}", source, key);
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            var processor = new BaseOutputProcessor();
            processor.OnData += s =>
            {
                if (String.IsNullOrEmpty(result))
                {
                    result = s;
                }
            };
            return new ProcessOutputManager(process, processor);
        }

        protected override void RaiseOnSuccess()
        {
            if (String.IsNullOrEmpty(result))
            {
                RaiseOnFailure();
            }
            else
            {
                base.RaiseOnSuccess();
            }
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git config get"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}