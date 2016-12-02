using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;


namespace GitHub.Unity
{
	class GitListUntrackedFilesTask : GitTask
	{
		public static void Schedule(Action<GitListUntrackedFilesTask> success, Action failure = null)
		{
			Tasks.Add(new GitListUntrackedFilesTask(success, failure));
		}


		public override bool Blocking { get { return false; } }
		public override bool Critical { get { return false; } }
		public override bool Cached { get { return false; } }
		public override string Label { get { return "git ls-files"; } }

		public IList<GitStatusEntry> Entries { get { return entries; } }


		protected override string ProcessArguments { get { return "ls-files -o --exclude-standard"; } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		Action<GitListUntrackedFilesTask> onSuccess;
		Action onFailure;
		StringWriter
			output = new StringWriter(),
			error = new StringWriter();
		List<GitStatusEntry> entries = new List<GitStatusEntry>();


		GitListUntrackedFilesTask(Action<GitListUntrackedFilesTask> success, Action failure = null)
		{
			onSuccess = success;
			onFailure = failure;
		}


		protected override void OnProcessOutputUpdate()
		{
			StringBuilder buffer = output.GetStringBuilder();
			int end = buffer.Length - 1;

			if(!Done)
			// Only try to avoid partial lines if the process did not already end
			{
				for(; end > 0 && buffer[end] != '\n'; --end);
			}

			if(end > 0)
			// Parse output lines into the entries list if we have any buffer to parse
			{
				for(int index = 0, last = -1; index <= end; ++index)
				{
					if(buffer[index] == '\n')
					{
						ParseOutputLine(last + 1, index);
						last = index;
					}
				}

				buffer.Remove(0, end + 1);
			}

			if(Done)
			// Process results when we are done
			{
				buffer = error.GetStringBuilder();
				if (buffer.Length > 0)
				// We failed. Make a little noise.
				{
					Tasks.ReportFailure(FailureSeverity.Moderate, this, buffer.ToString());
					if (onFailure != null)
					{
						Tasks.ScheduleMainThread(() => onFailure());
					}
				}
				else if (onSuccess != null)
				// We succeeded. Hand over the results!
				{
					Tasks.ScheduleMainThread(() => onSuccess(this));
				}
			}
		}


		void ParseOutputLine(int start, int end)
		{
			string path = output.GetStringBuilder().ToString(start, end - start);
			if (!entries.Any(e => e.Path.Equals(path)))
			{
				entries.Add(new GitStatusEntry(path, GitFileStatus.Untracked));
			}
		}
	}
}
