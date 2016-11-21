using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace GitHub.Unity
{
	class GitAddTask : ProcessTask
	{
		public static void Schedule(IEnumerable<string> files, Action onSuccess = null, Action onFailure = null)
		{
			Tasks.Add(new GitAddTask(files, onSuccess, onFailure));
		}


		StringWriter error = new StringWriter();
		string arguments = "";
		Action
			onSuccess,
			onFailure;


		public override bool Blocking { get { return false; } }
		public override bool Critical { get { return true; } }
		public override bool Cached { get { return true; } }
		public override string Label { get { return "git add"; } }


		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return arguments; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		public GitAddTask(IEnumerable<string> files, Action onSuccess = null, Action onFailure = null)
		{
			arguments = "add ";
			arguments += " -- ";

			foreach (string file in files)
			{
				arguments += " " + file;
			}

			this.onSuccess = onSuccess;
			this.onFailure = onFailure;
		}


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
				Tasks.ReportFailure(this, buffer.ToString());

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
