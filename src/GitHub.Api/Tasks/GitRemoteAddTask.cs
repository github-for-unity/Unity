using System;

namespace GitHub.Unity
{
    class GitRemoteAddTask : GitTask
    {
        private readonly string arguments;

        public GitRemoteAddTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
                string name, string url)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(name, "name");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            arguments = String.Format("remote add {0} {1}", name, url);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git remote add"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
