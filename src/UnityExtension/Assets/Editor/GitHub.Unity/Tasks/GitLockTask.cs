using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitLockTask : GitTask
    {
        private readonly string arguments;

        private GitLockTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            string path, Action onSuccess, Action onFailure = null)
            : base(environment, processManager, resultDispatcher,
                    str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");

            arguments = String.Format("lfs lock {0}", path);
        }

        public static void Schedule(string path, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitLockTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                path, onSuccess, onFailure));
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override bool Critical
        {
            get { return false; }
        }

        public override string Label
        {
            get { return "git lfs lock"; }
        }

        protected override string ProcessArguments
        {
            get { return arguments; }
        }
    }
}
