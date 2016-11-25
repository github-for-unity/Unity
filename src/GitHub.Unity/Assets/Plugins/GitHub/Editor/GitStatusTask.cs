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
		const string UnknownStatusKeyError = "Unknown file status key: '{0}'";


		// NOTE: Has to stay in sync with GitFileStatus enum for FileStatusFromKey to function as intended
		static readonly string[] GitFileStatusKeys = {
			"??",
			"M",
			"A",
			"D",
			"R",
			"C"
		};
		static Regex regex = new Regex(@"([AMRDC]|\?\?)(?:\d*)\s+([\w\d\/\.\-_ ]+)");


		public static bool TryParse(string line, out GitStatusEntry entry)
		{
			Match match = regex.Match(line);
			if (match.Groups.Count == 3)
			{
				string
					path = match.Groups[2].ToString(),
					statusKey = match.Groups[1].ToString();

				entry = new GitStatusEntry(path, FileStatusFromKey(statusKey));

				return true;
			}

			entry = new GitStatusEntry();

			return false;
		}


		static GitFileStatus FileStatusFromKey(string key)
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


		public readonly string
			Path,
			FullPath,
			ProjectPath;
		public readonly GitFileStatus Status;


		public GitStatusEntry(string path, GitFileStatus status)
		{
			Path = path;
			FullPath = Utility.RepositoryPathToAbsolute(Path);
			ProjectPath = Utility.RepositoryPathToAsset(Path);
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
		static Action<GitStatus> onStatusUpdate;
		static Regex branchRegex = new Regex(@"\#\#\s+([\w\d\/\.\-_ ]+)(?:\.\.\.([\w\d\/\.\-_ ]+))?");


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
			Utility.ParseLines(output.GetStringBuilder(), ParseOutputLine, Done);

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
			}
			status.Clear();
		}


		void ParseOutputLine(string line)
		{
			GitStatusEntry entry;

			// Grab change lines
			if (GitStatusEntry.TryParse(line, out entry))
			{
				if (!status.Entries.Any(e => e.Path.Equals(entry.Path)) && !Directory.Exists(Utility.RepositoryPathToAbsolute(entry.Path)))
				{
					status.Entries.Add(entry);
				}
				return;
			}


			// Grab local and remote branch
			Match match = branchRegex.Match(line);
			if (match.Groups.Count >= 2)
			{
				status.LocalBranch = match.Groups[1].ToString();
			}
			if (match.Groups.Count == 3)
			{
				status.RemoteBranch = match.Groups[2].ToString();
			}
		}
	}
}
