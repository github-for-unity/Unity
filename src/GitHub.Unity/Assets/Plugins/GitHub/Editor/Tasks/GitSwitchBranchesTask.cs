using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace GitHub.Unity
{
	class GitSwitchBranchesTask : GitTask
	{
		public static void Schedule(string branch, Action onSuccess, Action onFailure = null)
		{
			Tasks.Add(new GitSwitchBranchesTask(branch, onSuccess, onFailure));
		}


		string branch;
		Action
			onSuccess,
			onFailure;
		StringWriter
			output = new StringWriter(),
			error = new StringWriter();


		public override bool Blocking { get { return true; } }
		public override TaskQueueSetting Queued { get { return TaskQueueSetting.QueueSingle; } }
		public override bool Critical { get { return false; } }
		public override bool Cached { get { return false; } }
		public override string Label { get { return "git checkout"; } }

		protected override string ProcessArguments { get { return string.Format("checkout {0}", branch); } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		GitSwitchBranchesTask(string branch, Action onSuccess, Action onFailure = null)
		{
			this.branch = branch;
			this.onSuccess = onSuccess;
			this.onFailure = onFailure;
		}


		protected override void OnProcessOutputUpdate()
		{
			Utility.ParseLines(output.GetStringBuilder(), ParseOutputLine, Done);

			if (Done)
			{
				// Handle failure / success
				StringBuilder buffer = error.GetStringBuilder();
				if (buffer.Length > 0)
				{
					Tasks.ReportFailure(FailureSeverity.Critical, this, buffer.ToString());
					if (onFailure != null)
					{
						Tasks.ScheduleMainThread(onFailure);
					}
				}
				else
				{
					if (onSuccess != null)
					{
						Tasks.ScheduleMainThread(onSuccess);
					}
				}
			}
		}


		void ParseOutputLine(string line)
		{
			Debug.LogFormat("Line: '{0}'", line);
		}
	}
}
