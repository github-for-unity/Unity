using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitSwitchBranchesTask : GitTask
    {
        private const string SwitchConfirmedMessage = "Switched to branch '{0}'";

        private readonly string arguments;
        private readonly string branch;

        public GitSwitchBranchesTask(IEnvironment environment, IProcessManager processManager,
                ITaskResultDispatcher<string> resultDispatcher,
                string branch)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            this.branch = branch;
            arguments = String.Format("checkout {0}", branch);
        }

        protected override void OnCompleted()
        {
            // Handle failure / success
            var buffer = ErrorBuffer.GetStringBuilder();
            if (buffer.Length > 0)
            {
                var message = buffer.ToString().Trim();

                if (!message.Equals(String.Format(SwitchConfirmedMessage, branch)))
                {
                    RaiseOnFailure();
                    return;
                }
            }

            RaiseOnSuccess();
        }

        public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git checkout"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
