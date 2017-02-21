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

        public override bool Blocking { get { return true; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git push"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
