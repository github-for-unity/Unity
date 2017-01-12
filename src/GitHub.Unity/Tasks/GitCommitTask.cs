using System;
using System.Collections.Generic;
using System.IO;

namespace GitHub.Unity
{
    class GitCommitTask : GitTask
    {
        private string arguments = "";

        private GitCommitTask(string message, string body, Action onSuccess = null, Action onFailure = null)
            : base(str => onSuccess?.Invoke(), onFailure)
        {
            arguments = "commit ";
            arguments += String.Format(@" -m ""{0}{1}{2}""", message, Environment.NewLine, body);
        }

        public static void Schedule(IEnumerable<string> files, string message, string body, Action onSuccess = null, Action onFailure = null)
        {
            GitAddTask.Schedule(files, () => Schedule(message, body, onSuccess, onFailure), onFailure);
        }

        public static void Schedule(string message, string body, Action onSuccess = null, Action onFailure = null)
        {
            Tasks.Add(new GitCommitTask(message, body, onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            base.OnProcessOutputUpdate();

            // Always update
            StatusService.Instance.Run();
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override bool Critical
        {
            get { return true; }
        }

        public override bool Cached
        {
            get { return true; }
        }

        public override string Label
        {
            get { return "git commit"; }
        }

        protected override string ProcessArguments
        {
            get { return arguments; }
        }
    }
}
