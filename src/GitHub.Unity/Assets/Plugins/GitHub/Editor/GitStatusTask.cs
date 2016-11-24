using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
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


	struct GitStatus
	{
		public string
			LocalBranch,
			RemoteBranch;
		public List<GitStatusEntry> Entries;


		public void Clear()
		{
			LocalBranch = RemoteBranch = "";
			Entries.Clear();
		}
	}


	struct GitStatusEntry
	{
		public readonly string
			Path,
			FullPath,
			ProjectPath;
		public readonly GitFileStatus Status;


		public GitStatusEntry(string path, GitFileStatus status)
		{
			Path = path;
			FullPath = System.IO.Path.Combine(Utility.GitRoot, Path);
			string localDataPath = Utility.UnityDataPath.Substring(Utility.GitRoot.Length + 1);
			ProjectPath = (Path.IndexOf (localDataPath) == 0) ?
				("Assets" + Path.Substring(localDataPath.Length)).Replace(System.IO.Path.DirectorySeparatorChar, '/') :
				"";

			Status = status;
		}


		public override int GetHashCode()
		{
			return Path.GetHashCode();
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


		static Action<GitStatus> onStatusUpdate;


		public static void RegisterCallback(Action<GitStatus> callback)
		{
			onStatusUpdate += callback;
		}


		public static void UnregisterCallback(Action<GitStatus> callback)
		{
			onStatusUpdate -= callback;
		}


		public static void Schedule()
		{
			GitListUntrackedFilesTask.Schedule(task => Tasks.Add(new GitStatusTask(task.Entries)));
		}


		public override bool Blocking { get { return false; } }
		public override bool Critical { get { return false; } }
		public override bool Cached { get { return false; } }
		public override string Label { get { return "git status"; } }


		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return "status -b --porcelain"; } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		StringWriter
			output = new StringWriter(),
			error = new StringWriter();
		Regex
			changeRegex = new Regex(@"([AMRDC]|\?\?)\s+([\w\d\/\.\-_ ]+)"),
			branchRegex = new Regex(@"\#\#\s+([\w\d\/\.\-_ ]+)\.\.\.([\w\d\/\.\-_ ]+)");
		GitStatus status;


		GitStatusTask(IList<GitStatusEntry> existingEntries = null)
		{
			status.Entries = new List<GitStatusEntry>();
			if (existingEntries != null)
			{
				status.Entries.AddRange(existingEntries);
			}
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
			// If we are done, hand over the results to any listeners on the main thread
			{
				Tasks.ScheduleMainThread(DeliverResult);
			}
		}


		void DeliverResult()
		{
			if(onStatusUpdate != null)
			{
				onStatusUpdate(status);
				status.Clear();
			}
		}


		bool ParseOutputLine(int start, int end)
		{
			// TODO: Figure out how we get out of doing that ToString call
			string line = output.GetStringBuilder().ToString(start, (end - start) + 1);

			// Grab change lines
			Match match = changeRegex.Match(line);
			if (match.Groups.Count == 3)
			{
				string
					path = match.Groups[2].ToString(),
					statusKey = match.Groups[1].ToString();

				if (!status.Entries.Any(e => e.Path.Equals(path)) && !Directory.Exists(Path.Combine(Utility.GitRoot, path)))
				{
					status.Entries.Add(new GitStatusEntry(path, FileStatusFromKey(statusKey)));
				}
			}

			// Grab local and remote branch
			match = branchRegex.Match(line);
			if (match.Groups.Count >= 2)
			{
				status.LocalBranch = match.Groups[1].ToString();
			}
			if (match.Groups.Count == 3)
			{
				status.RemoteBranch = match.Groups[2].ToString();
			}

			return true;
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
