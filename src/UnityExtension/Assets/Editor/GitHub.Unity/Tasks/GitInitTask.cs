using System;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitInitTask : GitTask
    {
        private GitInitTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            Action onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher,
                    str => onSuccess.SafeInvoke(), onFailure)
        {}

        public static void Schedule(Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitInitTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git init"; } }
        protected override string ProcessArguments { get { return "init"; } }
    }
}
