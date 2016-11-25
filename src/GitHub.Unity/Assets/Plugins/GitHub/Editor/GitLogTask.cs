using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;


namespace GitHub.Unity
{
	struct GitLogEntry
	{
		const string
			Today = "Today",
			Yesterday = "Yesterday";

		public string
			CommitID,
			MergeA,
			MergeB,
			AuthorName,
			AuthorEmail,
			Summary,
			Description;
		public DateTimeOffset Time;
		public List<GitStatusEntry> Changes;


		public string ShortID
		{
			get
			{
				return CommitID.Length < 7 ? CommitID : CommitID.Substring(0, 7);
			}
		}


		public string PrettyTimeString
		{
			get
			{
				DateTimeOffset
					now = DateTimeOffset.Now,
					relative = Time.ToLocalTime();

				return string.Format(
					"{0}, {1:HH}:{1:mm}",
					relative.DayOfYear == now.DayOfYear ? Today :
						relative.DayOfYear == now.DayOfYear - 1 ? Yesterday :
							relative.ToString("d MMM yyyy"),
					relative
				);
			}
		}


		public void Clear()
		{
			CommitID = MergeA = MergeB = AuthorName = AuthorEmail = Summary = Description = "";
			Time = DateTimeOffset.Now;
			Changes = new List<GitStatusEntry>();
		}


		public override string ToString()
		{
			return string.Format(
@"CommitID: {0}
MergeA: {1}
MergeB: {2}
AuthorName: {3}
AuthorEmail: {4}
Time: {5}
Summary: {6}
Description: {7}",
				CommitID,
				MergeA,
				MergeB,
				AuthorName,
				AuthorEmail,
				Time.ToString(),
				Summary,
				Description
			);
		}
	}


	class GitLogTask : ProcessTask
	{
		enum ParsePhase
		{
			Commit,
			Author,
			Time,
			Description,
			Changes
		}


		const string
			UnhandledParsePhaseError = "Unhandled parse phase: '{0}'",
			LineParseError = "Log parse error in line: '{0}', parse phase: '{1}'",
			GitTimeFormat = "ddd MMM d HH:mm:ss yyyy zz";


		static Action<IList<GitLogEntry>> onLogUpdate;
		static Regex
			commitRegex = new Regex(@"commit\s(\S+)"),
			mergeRegex = new Regex(@"Merge:\s+(\S+)\s+(\S+)"),
			authorRegex = new Regex(@"Author:\s+(.+)\s<(.+)>"),
			timeRegex = new Regex(@"Date:\s+(.+)"),
			descriptionRegex = new Regex(@"^\s+(.+)");


		public static void RegisterCallback(Action<IList<GitLogEntry>> callback)
		{
			onLogUpdate += callback;
		}


		public static void UnregisterCallback(Action<IList<GitLogEntry>> callback)
		{
			onLogUpdate -= callback;
		}


		public static void Schedule(string file = null)
		{
			Tasks.Add(new GitLogTask(file));
		}


		string arguments = "log --name-status";
		StringWriter
			output = new StringWriter(),
			error = new StringWriter();
		List<GitLogEntry> entries = new List<GitLogEntry>();
		GitLogEntry parsedEntry = new GitLogEntry();
		ParsePhase parsePhase;
		bool completed = false;


		public override bool Blocking { get { return false; } }
		public override bool Critical { get { return false; } }
		public override bool Cached { get { return false; } }
		public override string Label { get { return "git log"; } }


		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return arguments; } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


		GitLogTask(string file = null)
		{
			parsedEntry.Clear();

			if (!string.IsNullOrEmpty(file))
			{
				arguments = string.Format("{0} --follow {1}", arguments, file);
			}
		}


		protected override void OnProcessOutputUpdate()
		{
			Utility.ParseLines(output.GetStringBuilder(), ParseOutputLine, Done);

			if (Done && !completed)
			{
				completed = true;

				// Handle failure / success
				StringBuilder buffer = error.GetStringBuilder();
				if (buffer.Length > 0)
				{
					Tasks.ReportFailure(FailureSeverity.Moderate, this, buffer.ToString());
				}
				else
				{
					Tasks.ScheduleMainThread(DeliverResult);
				}
			}
		}


		void DeliverResult()
		{
			if (onLogUpdate != null)
			{
				onLogUpdate(entries);
			}

			entries.Clear();
		}


		void ParseOutputLine(string line)
		{
			// Empty lines are section or commit dividers
			if (string.IsNullOrEmpty(line))
			{
				switch (parsePhase)
				{
					case ParsePhase.Changes:
						entries.Add(parsedEntry);
						parsedEntry.Clear();
						parsePhase = ParsePhase.Commit;
					return;
					default:
						++parsePhase;
					return;
				}
			}

			Match match;

			switch (parsePhase)
			{
				case ParsePhase.Commit:
					match = commitRegex.Match(line);
					if (match.Groups.Count == 2)
					{
						parsedEntry.CommitID = match.Groups[1].ToString();
						++parsePhase;
						return;
					}
				break;
				case ParsePhase.Author:
					// If this is a marge commit, merge info comes before author info, so we parse this and stay in the author phase
					match = mergeRegex.Match(line);
					if (match.Groups.Count == 3)
					{
						parsedEntry.MergeA = match.Groups[1].ToString();
						parsedEntry.MergeB = match.Groups[2].ToString();
						return;
					}

					match = authorRegex.Match(line);
					if (match.Groups.Count == 3)
					{
						parsedEntry.AuthorName = match.Groups[1].ToString();
						parsedEntry.AuthorEmail = match.Groups[2].ToString();
						++parsePhase;
						return;
					}
				break;
				case ParsePhase.Time:
					match = timeRegex.Match(line);
					if (match.Groups.Count == 2)
					{
						string time = match.Groups[1].ToString();

						parsedEntry.Time = DateTimeOffset.ParseExact(
							time,
							GitTimeFormat,
							System.Globalization.CultureInfo.InvariantCulture,
							System.Globalization.DateTimeStyles.None
						);

						if (DateTimeOffset.TryParseExact(
							time,
							GitTimeFormat,
							CultureInfo.InvariantCulture,
							DateTimeStyles.None,
							out parsedEntry.Time
						))
						{
							// NOTE: Time is always last in the header, so we should not progress to next phase here - the divider will do that
							return;
						}
					}
				break;
				case ParsePhase.Description:
					match = descriptionRegex.Match(line);
					if (match.Groups.Count == 2)
					{
						if (string.IsNullOrEmpty(parsedEntry.Summary))
						{
							parsedEntry.Summary = match.Groups[1].ToString();
						}
						else
						{
							parsedEntry.Description += match.Groups[1].ToString();
						}
						return;
					}
				break;
				case ParsePhase.Changes:
					GitStatusEntry entry;

					if (GitStatusEntry.TryParse(line, out entry))
					// Try to read the line as a change entry
					{
						parsedEntry.Changes.Add(entry);
						return;
					}
					else if ((match = commitRegex.Match(line)).Groups.Count == 2)
					// This commit had no changes, so complete parsing it and pass the next commit header into a new session
					{
						ParseOutputLine(null);
						ParseOutputLine(line);
						return;
					}
				break;
				default:
				throw new ApplicationException(string.Format(UnhandledParsePhaseError, parsePhase));
			}

			// Garbled input. Eject!
			Debug.LogErrorFormat(LineParseError, line, parsePhase);
			Abort();
		}
	}
}
