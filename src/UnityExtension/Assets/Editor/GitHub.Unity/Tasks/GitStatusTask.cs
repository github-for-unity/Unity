using System;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitStatusTask : GitTask
    {
        private readonly StatusOutputProcessor processor;
        private Action<GitStatus> callback;
        private GitStatus gitStatus;

        public GitStatusTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                IGitObjectFactory gitObjectFactory, Action<GitStatus> onSuccess, Action onFailure = null)
            : base(environment, processManager, resultDispatcher,
                  null, onFailure)
        {
            callback = onSuccess;
            processor = new StatusOutputProcessor(gitObjectFactory);
        }

        public static void Schedule(Action<GitStatus> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitStatusTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                EntryPoint.GitObjectFactory, onSuccess, onFailure));
        }

        protected override void OnOutputComplete(string output, string errors)
        {
            if (TaskResultDispatcher != null)
            {
                TaskResultDispatcher.ReportSuccess(DeliverResult);
            }
            else
            {
                DeliverResult();
            }
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnStatus += status => { gitStatus = status; };
            return new ProcessOutputManager(process, processor);
        }

        private void DeliverResult()
        {
            callback.SafeInvoke(gitStatus);
        }

        public override bool Blocking { get { return false; } }
        public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
        public override bool Critical { get { return false; } }
        public override bool Cached { get { return false; } }
        public override string Label { get { return "git status"; } }

        protected override string ProcessArguments
        {
            get { return "status -b -u --porcelain"; }
        }
    }
}
