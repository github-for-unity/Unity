using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class FindGitTask : ProcessTask
    {
        public FindGitTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            Action<string> onSuccess, Action onFailure = null)
            : base(environment, processManager, resultDispatcher, onSuccess, onFailure)
        {
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }

        public override string Label { get { return "find git"; } }

        protected override string ProcessName
        {
            get { return Environment.IsWindows ? "where" : "which"; }
        }

        protected override string ProcessArguments { get { return "git"; } }
    }
}
