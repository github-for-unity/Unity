using System;

namespace GitHub.Unity
{
    class GitTask : ProcessTask
    {
        private const string NoGitError = "Tried to run git task while git was not found.";

        public GitTask(Action<string> onSuccess, Action onFailure)
            : base(onSuccess, onFailure)
        {}

        public override void Run()
        {
            if (!Utility.GitFound)
            {
                Tasks.ReportFailure(FailureSeverity.Moderate, this, NoGitError);
                Abort();
                return;
            }

            base.Run();
        }

        protected override string ProcessName
        {
            get { return Utility.GitInstallPath; }
        }
    }
}
