namespace GitHub.Unity
{
    class GitLfsInstallTask : GitTask
    {
        public GitLfsInstallTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher)
            : base(environment, processManager, resultDispatcher)
        { }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git lfs install"; } }
        protected override string ProcessArguments { get { return "lfs install"; } }
    }
}