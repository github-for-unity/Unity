using GitHub.Api;
using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitAddTask : GitTask
    {
        private readonly string arguments;

        private GitAddTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            IEnumerable<string> files, Action onSuccess = null, Action onFailure = null)
            : base(environment, processManager, resultDispatcher, str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNull(files, "files");

            arguments = "add ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " " + file;
            }
        }

        public static void Schedule(IEnumerable<string> files, Action onSuccess = null, Action onFailure = null)
        {
            Tasks.Add(new GitAddTask(EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                files, onSuccess, onFailure));
        }

        protected override void OnOutputComplete(string output, string errors)
        {
            base.OnOutputComplete(output, errors);

            // Always update
            StatusService.Instance.Run();
        }

        public override bool Blocking { get { return false; } }
        public override string Label { get { return "git add"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
