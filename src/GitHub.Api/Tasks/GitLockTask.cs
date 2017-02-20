using System;

namespace GitHub.Unity
{
    class GitLockTask : GitTask
    {
        private readonly string arguments;

        private GitLockTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            string path, Action onSuccess, Action onFailure = null)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");

            arguments = String.Format("lfs lock {0}", path);
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
