namespace GitHub.Unity
{
    class FindGitTask : ProcessTask
    {
        public FindGitTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher<string> resultDispatcher)
            : base(environment, processManager, resultDispatcher)
        {}


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
