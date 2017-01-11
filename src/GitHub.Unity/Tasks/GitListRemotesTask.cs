using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

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
        public string Name;
        public string URL;
        public string Login;
        public string User;
        public string Token;
        public string Host;
        public GitRemoteFunction Function;

        public static bool TryParse(string line, out GitRemote result)
        {
            var match = Utility.ListRemotesRegex.Match(line);

            if (!match.Success)
            {
                result = new GitRemote();
                return false;
            }

            result = new GitRemote() {
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
            catch (Exception)
            {}

            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(String.Format("Name: {0}", Name));
            sb.AppendLine(String.Format("URL: {0}", URL));
            sb.AppendLine(String.Format("Login: {0}", Login));
            sb.AppendLine(String.Format("User: {0}", User));
            sb.AppendLine(String.Format("Token: {0}", Token));
            sb.AppendLine(String.Format("Host: {0}", Host));
            sb.AppendLine(String.Format("Function: {0}", Function));
            return sb.ToString();
        }
    }

    class GitListRemotesTask : GitTask
    {
        private const string ParseFailedError = "Remote parse error in line: '{0}'";

        private static Action<IList<GitRemote>> onRemotesListed;
        private List<GitRemote> entries = new List<GitRemote>();

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

        protected override void OnProcessOutputUpdate()
        {
            Utility.ParseLines(OutputBuffer.GetStringBuilder(), ParseOutputLine, Done);

            if (Done)
            {
                // Handle failure / success
                var buffer = ErrorBuffer.GetStringBuilder();
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

        private void DeliverResult()
        {
            if (onRemotesListed != null)
            {
                onRemotesListed(entries);
            }

            entries.Clear();
        }

        private void ParseOutputLine(string line)
        {
            // Parse line as a remote
            GitRemote remote;
            if (GitRemote.TryParse(line, out remote))
            {
                // Join Fetch/Push entries into single Both entries
                if (remote.Function != GitRemoteFunction.Unknown &&
                    entries.RemoveAll(
                        e => e.Function != GitRemoteFunction.Unknown && e.Function != remote.Function && e.Name.Equals(remote.Name)) > 0)
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

        public override bool Blocking
        {
            get { return false; }
        }

        public virtual TaskQueueSetting Queued
        {
            get { return TaskQueueSetting.QueueSingle; }
        }

        public override bool Critical
        {
            get { return false; }
        }

        public override bool Cached
        {
            get { return false; }
        }

        public override string Label
        {
            get { return "git remote"; }
        }

        protected override string ProcessArguments
        {
            get { return "remote -v"; }
        }
    }
}
