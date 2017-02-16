using System;
using System.Collections.Generic;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitRemoteListTask : GitTask
    {
        private Action<IList<GitRemote>> callback;
        private RemoteListOutputProcessor processor;

        private List<GitRemote> remotes = new List<GitRemote>();

        public GitRemoteListTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                Action<IList<GitRemote>> onSuccess, Action onFailure = null)
            : base(environment, processManager, resultDispatcher,
                  null, onFailure)
        {
            processor = new RemoteListOutputProcessor();
            callback = onSuccess;
        }

        protected override void OnOutputComplete(string output, string errors)
        {
            if (TaskResultDispatcher != null)
            {
                TaskResultDispatcher.ReportSuccess(() => callback?.Invoke(remotes));
            }
            else
            {
                callback?.Invoke(remotes);
            }
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnRemote += AddRemote;
            return new ProcessOutputManager(process, processor);
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
