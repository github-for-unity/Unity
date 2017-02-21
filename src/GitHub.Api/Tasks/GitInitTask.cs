namespace GitHub.Unity
{
    class GitInitTask : GitTask
    {
        public GitInitTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher)
            : base(environment, processManager, resultDispatcher)
        {}

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git init"; } }
        protected override string ProcessArguments { get { return "init"; } }
    }
}
