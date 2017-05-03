using System;

namespace GitHub.Unity
{
    class GitPushTask : GitNetworkTask
    {
        private readonly string arguments;

        public GitPushTask(IEnvironment environment, IProcessManager processManager,
                ITaskResultDispatcher<string> resultDispatcher, ICredentialManager credentialManager,
                IUIDispatcher uiDispatcher)
            : base(environment, processManager, resultDispatcher, credentialManager, uiDispatcher)
        {
            arguments = "push";
        }

        public GitPushTask(
                IEnvironment environment, IProcessManager processManager,
                ITaskResultDispatcher<string> resultDispatcher, ICredentialManager credentialManager,
                IUIDispatcher uiDispatcher,
                string remote, string branch, bool setUpstream)
            : base(environment, processManager, resultDispatcher, credentialManager, uiDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            arguments = String.Format("push {0} {1} {2}:{2}",
                setUpstream ? "-u" : "",
                remote, branch);
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
            if (String.IsNullOrEmpty(line))
                return;
            if (line.StartsWith("fatal:"))
            {
                ErrorBuffer.WriteLine(line);
            }
        }

        public override bool Blocking => true;
        public override bool Critical => false;
        public override string Label { get { return "git push"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
