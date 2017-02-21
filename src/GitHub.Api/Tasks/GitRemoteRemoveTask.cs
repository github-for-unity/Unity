using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitRemoteRemoveTask : GitTask
    {
        private readonly string arguments;

        public GitRemoteRemoveTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher<string> resultDispatcher,
            string remote)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            arguments = String.Format("remote rm {0}", remote);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git remote rm"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
