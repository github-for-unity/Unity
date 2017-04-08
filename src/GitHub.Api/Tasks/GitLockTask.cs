using System;

namespace GitHub.Unity
{
    class GitLockTask : GitTask
    {
        private readonly string arguments;

        public GitLockTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            string path)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            arguments = String.Format("lfs lock {0}", path);
        }

        public override bool Blocking => false;
        public override bool Critical => false;
        public override string Label { get; set; } = "git lfs lock";
        protected override string ProcessArguments => arguments;
    }
}
