using System;
using System.Text;
using GitHub.Extensions;

namespace GitHub.Unity
{
    class GitFetchTask : GitNetworkTask
    {
        private readonly string arguments;

        public GitFetchTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            IKeychainManager keychainManager, IUIDispatcher uiDispatcher,
            string remote)
            : base(environment, processManager, resultDispatcher, keychainManager, uiDispatcher)
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

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            var processor = new BaseOutputProcessor();
            processor.OnData += OutputBuffer.WriteLine;
            process.OnErrorData += Process_OnErrorData;
            return new ProcessOutputManager(process, processor);
        }

        private void Process_OnErrorData(string line)
        {
            if (line.IsNullOrWhiteSpace())
            {
                return;
            }

            if (line.StartsWith("fatal:"))
            {
                ErrorBuffer.WriteLine(line);
            }
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git fetch"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}