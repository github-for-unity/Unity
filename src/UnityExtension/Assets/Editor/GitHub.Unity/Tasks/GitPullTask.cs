using System;
using System.Text;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitPullTask : GitNetworkTask
    {
        private readonly string arguments;

        private GitPullTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            ICredentialManager credentialManager, IUIDispatcher uiDispatcher,
            string remote, string branch,
            Action onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher, credentialManager, uiDispatcher,
                  str => onSuccess.SafeInvoke(), onFailure)
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

        public static void Schedule(string remote, string branch,
            Action onSuccess, Action onFailure = null)
        {
            var uiDispatcher = new AuthenticationUIDispatcher();
            Tasks.Add(new GitPullTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                EntryPoint.CredentialManager, uiDispatcher,
                remote, branch, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git pull"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
