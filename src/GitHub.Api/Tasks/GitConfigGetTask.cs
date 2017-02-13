using System;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitConfigGetTask : GitTask
    {
        private readonly string arguments;

        public GitConfigGetTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            string key, GitConfigSource configSource, Action<string> onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher,
                  onSuccess, onFailure)
        {
            var source = "";
            if (configSource != GitConfigSource.NonSpecified)
            {
                source = "--";
                source += configSource == GitConfigSource.Local
                    ? "local"
                    : (configSource == GitConfigSource.User
                        ? "system"
                        : "global");
            }
            arguments = String.Format("config {0} --get {1}", key, source);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git config get"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}