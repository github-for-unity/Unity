using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace GitHub.Unity
{
	class GitCommitTask : ProcessTask
	{
		const string
			CommitErrorTitle = "GitHub",
			CommitErrorMessage = "Git comit failed:\n{0}",
			CommitErrorOK = "OK";


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


		public GitCommitTask(string message, string body, Action onSuccess = null, Action onFailure = null)
		{
			arguments = "commit ";
			arguments += string.Format(@" -m ""{0}\n{1}""", message, body);

			this.onSuccess = onSuccess;
			this.onFailure = onFailure;
		}


		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return arguments; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		protected override void OnProcessOutputUpdate()
		{
			if (!Done)
			{
				return;
			}

			// Pop up any errors and report success or failure
			StringBuilder buffer = error.GetStringBuilder();
			if (buffer.Length > 0)
			{
				Tasks.ScheduleMainThread(() =>
				{
					EditorUtility.DisplayDialog(CommitErrorTitle, string.Format(CommitErrorMessage, buffer.ToString()), CommitErrorOK);

					if (onFailure != null)
					{
						onFailure();
					}
				});
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
