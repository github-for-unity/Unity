using GitHub.Api;
using System;

namespace GitHub.Unity
{
    class GitRemoteAddTask : GitTask
    {
        private readonly string arguments;

        private GitRemoteAddTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                string name, string url, Action onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher,
                  str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(name, "name");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            arguments = String.Format("remote add {0} {1}", name, url);
        }

        public static void Schedule(string name, string url, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitRemoteAddTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                name, url, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git remote add"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
