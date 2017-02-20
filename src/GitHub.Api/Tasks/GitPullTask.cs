using System;
using System.Text;

namespace GitHub.Unity
{
    class GitPullTask : GitNetworkTask
    {
        private readonly string arguments;

        public GitPullTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            ICredentialManager credentialManager, IUIDispatcher uiDispatcher,
            string remote, string branch)
            : base(environment, processManager, resultDispatcher, credentialManager, uiDispatcher)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("pull");

            if (!String.IsNullOrEmpty(remote))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(remote);
            }

            if (!String.IsNullOrEmpty(branch))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(branch);
            }

            arguments = stringBuilder.ToString();
        }

        public override bool Blocking { get { return true; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git pull"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
