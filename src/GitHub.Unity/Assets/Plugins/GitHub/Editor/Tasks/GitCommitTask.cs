using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace GitHub.Unity
{
    class GitCommitTask : GitTask
    {
        public static void Schedule(IEnumerable<string> files, string message, string body, Action onSuccess = null, Action onFailure = null)
        {
            GitAddTask.Schedule(files, () => Schedule(message, body, onSuccess, onFailure), onFailure);
        }


        public static void Schedule(string message, string body, Action onSuccess = null, Action onFailure = null)
        {
            Tasks.Add(new GitCommitTask(message, body, onSuccess, onFailure));
        }


        StringWriter error = new StringWriter();
        string arguments = "";
        Action
            onSuccess,
            onFailure;


        GitCommitTask(string message, string body, Action onSuccess = null, Action onFailure = null)
        {
            arguments = "commit ";
            arguments += string.Format(@" -m ""{0}{1}{2}""", message, Environment.NewLine, body);

            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
        }


        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return true; } }
        public override bool Cached { get { return true; } }
        public override string Label { get { return "git commit"; } }


        protected override string ProcessArguments { get { return arguments; } }
        protected override TextWriter ErrorBuffer { get { return error; } }


        protected override void OnProcessOutputUpdate()
        {
            if (!Done)
            {
                return;
            }

            // Handle failure / success
            StringBuilder buffer = error.GetStringBuilder();
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
    }
}
