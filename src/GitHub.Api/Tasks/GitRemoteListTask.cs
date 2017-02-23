using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitRemoteListTask : GitTask
    {
        private readonly ITaskResultDispatcher<IEnumerable<GitRemote>> resultDispatcher;
        private RemoteListOutputProcessor processor;

        private List<GitRemote> remotes = new List<GitRemote>();

        public GitRemoteListTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<IEnumerable<GitRemote>> resultDispatcher)
            : base(environment, processManager)
        {
            this.resultDispatcher = resultDispatcher;
            processor = new RemoteListOutputProcessor();
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnRemote += AddRemote;
            return new ProcessOutputManager(process, processor);
        }

        protected override void RaiseOnSuccess()
        {
            resultDispatcher.ReportSuccess(remotes);
        }

        private void AddRemote(GitRemote remote)
        {
            remotes.Add(remote);
        }

        public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git remote"; } }
        protected override string ProcessArguments { get { return "remote -v"; } }
    }
}
