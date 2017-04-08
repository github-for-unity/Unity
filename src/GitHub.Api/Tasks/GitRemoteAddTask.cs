using System;

namespace GitHub.Unity
{
    class GitRemoteChangeTask : GitTask
    {
        private readonly string arguments;

        public GitRemoteChangeTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
                string remote, string url)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            arguments = String.Format("remote set-url {0} {1}", remote, url);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git remote set-url"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }


    class GitRemoteAddTask : GitTask
    {
        private readonly string arguments;

        public GitRemoteAddTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
                string remote, string url)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            arguments = String.Format("remote add {0} {1}", remote, url);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git remote add"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
