using System;
using System.Collections.Generic;
using System.IO;

namespace GitHub.Unity
{
    class GitAddTask : GitTask
    {
        private string arguments = "";
        private Action onFailure;
        private Action onSuccess;

        private GitAddTask(IEnumerable<string> files, Action onSuccess = null, Action onFailure = null)
        {
            arguments = "add ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " " + file;
            }

            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
        }

        public static void Schedule(IEnumerable<string> files, Action onSuccess = null, Action onFailure = null)
        {
            Tasks.Add(new GitAddTask(files, onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            if (!Done)
            {
                return;
            }

            // Handle failure / success
            var buffer = ErrorBuffer.GetStringBuilder();
            if (buffer.Length > 0)
            {
                Tasks.ReportFailure(FailureSeverity.Critical, this, buffer.ToString());

                if (onFailure != null)
                {
                    Tasks.ScheduleMainThread(() => onFailure());
                }
            }
            else if (onSuccess != null)
            {
                Tasks.ScheduleMainThread(() => onSuccess());
            }

            // Always update
            GitStatusTask.Schedule();
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
            get { return "git add"; }
        }

        protected override string ProcessArguments
        {
            get { return arguments; }
        }
    }
}
