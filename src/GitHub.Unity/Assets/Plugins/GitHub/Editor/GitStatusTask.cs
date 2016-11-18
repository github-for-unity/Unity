using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace GitHub.Unity
{
	class GitStatusTaskRunner : AssetPostprocessor
	{
		[InitializeOnLoadMethod]
		static void OnLoad()
		{
			Tasks.ScheduleMainThread(() => GitStatusTask.Schedule());
		}


		static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveSource)
		{
			GitStatusTask.Schedule();
		}
	}

	enum GitFileStatus
	{
		Untracked,
		Modified,
		Added,
		Deleted,
		Renamed,
		Copied
	}


	struct GitStatusEntry
	{
		string path;
		public string Path { get { return path; } }
		GitFileStatus status;
		public GitFileStatus Status { get { return status; } }


		public GitStatusEntry(string path, GitFileStatus status)
		{
			this.path = path;
			this.status = status;
		}


		public override string ToString()
		{
			return string.Format("'{0}': {1}", Path, Status);
		}
	}


	class GitStatusTask : ProcessTask
	{
		const string UnknownStatusKeyError = "Unknown file status key: '{0}'";


		// NOTE: Has to stay in sync with GitFileStatus enum for FileStatusFromKey to function as intended
		readonly string[] GitFileStatusKeys = {
			"??",
			"M",
			"A",
			"D",
			"R",
			"C"
		};


		static Action<IList<GitStatusEntry>> onStatusUpdate;


		public static void RegisterCallback(Action<IList<GitStatusEntry>> callback)
		{
			onStatusUpdate += callback;
		}


		public static void UnregisterCallback(Action<IList<GitStatusEntry>> callback)
		{
			onStatusUpdate -= callback;
		}


		public static void Schedule()
		{
			Tasks.Add(new GitStatusTask());
		}


		public override bool Blocking { get { return false; } }
		public override bool Critical { get { return false; } }
		public override bool Cached { get { return false; } }
		public override string Label { get { return "git status"; } }


		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return "status --porcelain"; } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		StringWriter
			output = new StringWriter(),
			error = new StringWriter();
		Regex lineRegex = new Regex(@"([AMRDC]|\?\?)\s+([\w\d\/\.\-_ ]+)");
		List<GitStatusEntry> entries = new List<GitStatusEntry>();


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
			// If we are done, hand over the results to any listeners on the main thread
			{
				Tasks.ScheduleMainThread(DeliverResult);
			}
		}


		void DeliverResult()
		{
			if(onStatusUpdate != null)
			{
				onStatusUpdate(entries);
				entries.Clear();
			}
		}


		void ParseOutputLine(int start, int end)
		{
			StringBuilder buffer = output.GetStringBuilder();

			// TODO: Figure out how we get out of doing that ToString call
			Match match = lineRegex.Match(buffer.ToString(start, (end - start) + 1));

			if(match.Groups.Count < 3)
			{
				return;
			}

			entries.Add(new GitStatusEntry(match.Groups[2].ToString(), FileStatusFromKey(match.Groups[1].ToString())));
		}


		GitFileStatus FileStatusFromKey(string key)
		{
			for(int index = 0; index < GitFileStatusKeys.Length; ++index)
			{
				if(key.Equals(GitFileStatusKeys[index]))
				{
					return (GitFileStatus)index;
				}
			}

			throw new ArgumentException(string.Format(UnknownStatusKeyError, key));
		}
	}
}
