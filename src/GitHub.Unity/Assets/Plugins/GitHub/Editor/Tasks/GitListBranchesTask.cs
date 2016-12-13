using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;


namespace GitHub.Unity
{
	struct GitBranchList
	{
		public string[] Branches{ get; private set; }
		public int ActiveIndex{ get; private set; }


		public GitBranchList(IEnumerable<string> branches, int activeIndex)
		{
			Branches = branches.ToArray();
			ActiveIndex = activeIndex;
		}
	}


	class GitListBranchesTask : GitTask
	{
		enum Mode
		{
			Local,
			Remote
		}


		const string
			LocalArguments = "branch",
			RemoteArguments = "branch -r",
			UnmatchedLineError = "Unable to match the line '{0}'";
		static Regex regex = new Regex(@"^(?<active>\*)?\s+(?<name>[\w\d\/\-\_]+)");


		public static void ScheduleLocal(Action<GitBranchList> onSuccess, Action onFailure = null)
		{
			Schedule(Mode.Local, onSuccess, onFailure);
		}


		public static void ScheduleRemote(Action<GitBranchList> onSuccess, Action onFailure = null)
		{
			Schedule(Mode.Remote, onSuccess, onFailure);
		}


		static void Schedule(Mode mode, Action<GitBranchList> onSuccess, Action onFailure = null)
		{
			Tasks.Add(new GitListBranchesTask(mode, onSuccess, onFailure));
		}


		StringWriter
			output = new StringWriter(),
			error = new StringWriter();
		Mode mode;
		List<string> branches = new List<string>();
		int activeIndex;
		Action<GitBranchList> onSuccess;
		Action onFailure;


		public override bool Blocking { get { return false; } }
		public override TaskQueueSetting Queued { get { return TaskQueueSetting.Queue; } }
		public override bool Critical { get { return false; } }
		public override bool Cached { get { return false; } }
		public override string Label { get { return "git branch"; } }


		protected override string ProcessArguments { get { return mode == Mode.Local ? LocalArguments : RemoteArguments; } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		GitListBranchesTask(Mode mode, Action<GitBranchList> onSuccess, Action onFailure = null)
		{
			this.mode = mode;
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
					Tasks.ReportFailure(FailureSeverity.Moderate, this, buffer.ToString());
					if (onFailure != null)
					{
						Tasks.ScheduleMainThread(() => onFailure());
					}
				}
				else
				{
					Tasks.ScheduleMainThread(DeliverResult);
				}
			}
		}


		void DeliverResult()
		{
			if (onSuccess == null)
			{
				return;
			}

			onSuccess(new GitBranchList(branches.ToArray(), activeIndex));
		}


		void ParseOutputLine(string line)
		{
			Match match = regex.Match(line);

			if (!match.Success)
			{
				Tasks.ReportFailure(FailureSeverity.Moderate, this, string.Format(UnmatchedLineError, line));
				return;
			}

			if (!string.IsNullOrEmpty(match.Groups["active"].Value))
			{
				activeIndex = branches.Count;
			}

			branches.Add(match.Groups["name"].Value);
		}
	}
}
