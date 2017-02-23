using System;

namespace GitHub.Unity
{
    class GitCommitTask : GitTask
    {
        private readonly string arguments;

        public GitCommitTask(IEnvironment environment, IProcessManager processManager,
                ITaskResultDispatcher<string> resultDispatcher,
                string message, string body)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(message, "message");

            arguments = "commit ";
            arguments += String.Format(" -m \"{0}", message);
            if (!String.IsNullOrEmpty(body))
                arguments += String.Format("{0}{1}", Environment.NewLine, body);
            arguments += "\"";
        }

        public override bool Blocking { get { return false; } }
        public override string Label { get { return "git commit"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
