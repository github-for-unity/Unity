namespace GitHub.Unity
{
    class GitTask : ProcessTask
    {
        const string NoGitError = "Tried to run git task while git was not found.";


        protected override string ProcessName { get { return Utility.GitInstallPath; } }


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
    }
}
