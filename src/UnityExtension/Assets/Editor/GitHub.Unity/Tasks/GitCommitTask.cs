using System;
using System.Collections.Generic;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitCommitTask : GitTask
    {
        private readonly string arguments;

        private GitCommitTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                            string message, string body, Action onSuccess = null, Action onFailure = null)
            : base(environment, processManager, resultDispatcher,
                  str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(message, "message");

            arguments = "commit ";
            arguments += String.Format(" -m \"{0}", message);
            if (!String.IsNullOrEmpty(body))
                arguments += String.Format("{0}{1}", Environment.NewLine, body);
            arguments += "\"";
        }

        public static void Schedule(IEnumerable<string> files, string message, string body, Action onSuccess = null, Action onFailure = null)
        {
            GitAddTask.Schedule(files, () => Schedule(message, body, onSuccess, onFailure), onFailure);
        }

        public static void Schedule(string message, string body, Action onSuccess = null, Action onFailure = null)
        {
            Tasks.Add(new GitCommitTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                message, body, onSuccess, onFailure));
        }

        protected override void OnOutputComplete(string output, string errors)
        {
            base.OnOutputComplete(output, errors);

            // Always update
            StatusService.Instance.Run();
        }

        public override bool Blocking { get { return false; } }
        public override string Label { get { return "git commit"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
