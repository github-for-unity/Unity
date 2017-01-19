using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GitHub.Unity
{
    class GitListRemotesTask : GitTask
    {
        private const string ParseFailedError = "Remote parse error in line: '{0}'";

        private static Action<IList<GitRemote>> onRemotesListed;
        private List<GitRemote> entries = new List<GitRemote>();
        private Action<string> onSuccess;

        public GitListRemotesTask()
            : base(null, null)
        {
            this.onSuccess = ProcessOutput;
        }

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

        private void DeliverResult()
        {
            if (onRemotesListed != null)
            {
                onRemotesListed(entries);
            }

            entries.Clear();
        }

        private void ProcessOutput(string value)
        {
            foreach (var line in value.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
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
                    Logging.Logger.LogWarningFormat(ParseFailedError, line);
                }
            }
            InternalInvoke();
        }

        private void InternalInvoke()
        {
            Tasks.ScheduleMainThread(DeliverResult);
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override TaskQueueSetting Queued
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

        protected override Action<string> OnSuccess { get { return onSuccess; } }
    }
}
