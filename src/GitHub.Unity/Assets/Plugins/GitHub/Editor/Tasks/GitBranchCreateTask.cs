using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace GitHub.Unity
{
	class GitBranchCreateTask : GitTask
	{
		public static void Schedule(string newBranch, string baseBranch, Action onSuccess, Action onFailure = null)
		{
			Tasks.Add(new GitBranchCreateTask(newBranch, baseBranch, onSuccess, onFailure));
		}


		string
			newBranch,
			baseBranch;
		Action
			onSuccess,
			onFailure;
		StringWriter
			output = new StringWriter(),
			error = new StringWriter();


		public override bool Blocking { get { return false; } }
		public override TaskQueueSetting Queued { get { return TaskQueueSetting.Queue; } }
		public override bool Critical { get { return false; } }
		public override bool Cached { get { return true; } }
		public override string Label { get { return "git branch"; } }

		protected override string ProcessArguments { get { return string.Format("branch {0} {1}", newBranch, baseBranch); } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		GitBranchCreateTask(string newBranch, string baseBranch, Action onSuccess, Action onFailure)
		{
			this.newBranch = newBranch;
			this.baseBranch = baseBranch;
			this.onSuccess = onSuccess;
			this.onFailure = onFailure;
		}


		protected override void OnProcessOutputUpdate()
		{
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

					return;
				}

				if (onSuccess != null)
				{
					Tasks.ScheduleMainThread(onSuccess);
				}
			}
		}
	}
}
