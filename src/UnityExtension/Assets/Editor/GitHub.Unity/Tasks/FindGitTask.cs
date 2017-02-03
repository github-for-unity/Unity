using System;
using System.IO;
using GitHub.Unity.Logging;

namespace GitHub.Unity
{
    class FindGitTask : ProcessTask
    {
        private static readonly ILogger FindGitTaskLogger = Logging.Logger.GetLogger<FindGitTask>();

        private FindGitTask(Action<string> onSuccess, Action onFailure = null)
            : base(onSuccess, onFailure)
        {
        }

        public static void Schedule(Action<string> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new FindGitTask(onSuccess, onFailure));
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override bool Critical
        {
            get { return false; }
        }

        public override bool Cached
        {
            get { return false; }
        }

        public override string Label
        {
            get { return "find git"; }
        }

        protected override string ProcessName
        {
            get { return Utility.IsWindows ? "where" : "which"; }
        }

        protected override string ProcessArguments
        {
            get { return "git"; }
        }
    }
}
