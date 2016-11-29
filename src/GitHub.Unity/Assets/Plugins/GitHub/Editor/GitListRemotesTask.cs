using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace GitHub.Unity
{
	enum GitRemoteFunction
	{
		Unknown,
		Fetch,
		Push,
		Both
	}


	struct GitRemote
	{
		static Regex regex = new Regex(
			@"(?<name>[\w\d\-\_]+)\s+(?<url>https?:\/\/(?<login>(?<user>[\w\d]+)(?::(?<token>[\w\d]+))?)@(?<host>[\w\d\.\/\%]+))\s+\((?<function>fetch|push)\)"
		);


		public static bool TryParse(string line, out GitRemote result)
		{
			Match match = regex.Match(line);

			if (!match.Success)
			{
				result = new GitRemote();
				return false;
			}

			result = new GitRemote()
			{
				Name = match.Groups["name"].Value,
				URL = match.Groups["url"].Value,
				Login = match.Groups["login"].Value,
				User = match.Groups["user"].Value,
				Token = match.Groups["token"].Value,
				Host = match.Groups["host"].Value
			};

			try
			{
				result.Function = (GitRemoteFunction)Enum.Parse(typeof(GitRemoteFunction), match.Groups["function"].Value, true);
			}
			catch(Exception)
			{}

			return true;
		}


		public string
			Name,
			URL,
			Login,
			User,
			Token,
			Host;
		public GitRemoteFunction Function;


		public override string ToString()
		{
			return string.Format(
@"Name: {0}
URL: {1}
Login: {2}
User: {3}
Token: {4}
Host: {5}
Function: {6}",
				Name,
				URL,
				Login,
				User,
				Token,
				Host,
				Function
			);
		}
	}


	class GitListRemotesTask : ProcessTask
	{
		const string ParseFailedError = "Remote parse error in line: '{0}'";


		static Action<IList<GitRemote>> onRemotesListed;


		public static void RegisterCallback(Action<IList<GitRemote>> callback)
		{
			onRemotesListed += callback;
		}


		public static void UnregisterCallback(Action<IList<GitRemote>> callback)
		{
			onRemotesListed -= callback;
		}


		public static void Schedule()
		{
			Tasks.Add(new GitListRemotesTask());
		}


		StringWriter
			output = new StringWriter(),
			error = new StringWriter();
		List<GitRemote> entries = new List<GitRemote>();


		public override bool Blocking { get { return false; } }
		public override bool Critical { get { return false; } }
		public override bool Cached { get { return false; } }
		public override string Label { get { return "git remote"; } }


		protected override string ProcessName { get { return "git"; } }
		protected override string ProcessArguments { get { return "remote -v"; } }
		protected override TextWriter OutputBuffer { get { return output; } }
		protected override TextWriter ErrorBuffer { get { return error; } }


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
				}
				else
				{
					Tasks.ScheduleMainThread(DeliverResult);
				}
			}
		}


		void DeliverResult()
		{
			if (onRemotesListed != null)
			{
				onRemotesListed(entries);
			}

			entries.Clear();
		}


		void ParseOutputLine(string line)
		{
			// Parse line as a remote
			GitRemote remote;
			if (GitRemote.TryParse(line, out remote))
			{
				// Join Fetch/Push entries into single Both entries
				if (
					remote.Function != GitRemoteFunction.Unknown &&
					entries.RemoveAll(e =>
						e.Function != GitRemoteFunction.Unknown && e.Function != remote.Function && e.Name.Equals(remote.Name)
					) > 0
				)
				{
					remote.Function = GitRemoteFunction.Both;
				}

				// Whatever the case, list the remote
				entries.Add(remote);
			}
			else
			{
				Debug.LogWarningFormat(ParseFailedError, line);
			}
		}
	}
}
