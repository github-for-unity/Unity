using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHub.Unity
{
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
        public string LocalBranch, RemoteBranch;
        public int Ahead, Behind;
        public List<GitStatusEntry> Entries;

        public void Clear()
        {
            LocalBranch = RemoteBranch = "";
            Entries.Clear();
        }
    }

    struct GitStatusEntry
    {
        private const string UnknownStatusKeyError = "Unknown file status key: '{0}'";

        // NOTE: Has to stay in sync with GitFileStatus enum for FileStatusFromKey to function as intended
        private static readonly string[] GitFileStatusKeys = { "??", "M", "A", "D", "R", "C" };

        public static bool TryParse(string line, out GitStatusEntry entry)
        {
            var match = Utility.StatusStartRegex.Match(line);
            string statusKey = match.Groups["status"].Value, path = match.Groups["path"].Value;

            if (!string.IsNullOrEmpty(statusKey) && !string.IsNullOrEmpty(path))
            {
                var status = FileStatusFromKey(statusKey);
                var renameIndex = line.IndexOf(Utility.StatusRenameDivider);

                if (renameIndex >= 0)
                {
                    match = Utility.StatusEndRegex.Match(line.Substring(renameIndex));
                    entry = new GitStatusEntry(match.Groups["path"].Value, status, path.Substring(0, path.Length - 1));
                }
                else
                {
                    entry = new GitStatusEntry(path, status);
                }

                return true;
            }

            entry = new GitStatusEntry();

            return false;
        }

        private static GitFileStatus FileStatusFromKey(string key)
        {
            for (var index = 0; index < GitFileStatusKeys.Length; ++index)
            {
                if (key.Equals(GitFileStatusKeys[index]))
                {
                    return (GitFileStatus)index;
                }
            }

            throw new ArgumentException(String.Format(UnknownStatusKeyError, key));
        }

        public readonly string Path, FullPath, ProjectPath, OriginalPath;
        public readonly GitFileStatus Status;

        public GitStatusEntry(string path, GitFileStatus status, string originalPath = "")
        {
            Path = path;
            FullPath = Utility.RepositoryPathToAbsolute(Path);
            ProjectPath = Utility.RepositoryPathToAsset(Path);
            Status = status;
            OriginalPath = originalPath;
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("'{0}': {1}", Path, Status);
        }
    }

    class GitStatusTask : GitTask
    {
        private const string BranchNamesSeparator = "...";

        private static Action<GitStatus> onStatusUpdate;
        private GitStatus status;

        private GitStatusTask(IList<GitStatusEntry> existingEntries = null)
        {
            status.Entries = new List<GitStatusEntry>();
            if (existingEntries != null)
            {
                status.Entries.AddRange(existingEntries);
            }
        }

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

        protected override void OnProcessOutputUpdate()
        {
            Utility.ParseLines(OutputBuffer.GetStringBuilder(), ParseOutputLine, Done);

            // If we are done, hand over the results to any listeners on the main thread
            if (Done)
            {
                Tasks.ScheduleMainThread(DeliverResult);
            }
        }

        private void DeliverResult()
        {
            if (onStatusUpdate != null)
            {
                onStatusUpdate(status);
            }
            status.Clear();
        }

        private void ParseOutputLine(string line)
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
            if (Utility.StatusBranchLineValidRegex.Match(line).Success)
            {
                var index = line.IndexOf(BranchNamesSeparator);

                // Remote branch available
                if (index >= 0)
                {
                    status.LocalBranch = line.Substring(2, index - 2);
                    status.RemoteBranch = line.Substring(index + BranchNamesSeparator.Length);
                    index = status.RemoteBranch.IndexOf('[');

                    // Ahead and/or behind information available
                    if (index > 0)
                    {
                        var match = Utility.StatusAheadBehindRegex.Match(status.RemoteBranch.Substring(index - 1));

                        status.RemoteBranch = status.RemoteBranch.Substring(0, index).Trim();

                        string aheadString = match.Groups["ahead"].Value, behindString = match.Groups["behind"].Value;

                        status.Ahead = string.IsNullOrEmpty(aheadString) ? 0 : Int32.Parse(aheadString);
                        status.Behind = string.IsNullOrEmpty(behindString) ? 0 : Int32.Parse(behindString);
                    }
                    else
                    {
                        status.RemoteBranch = status.RemoteBranch.Trim();
                    }
                }
                // No remote branch
                else
                {
                    status.LocalBranch = line.Substring(2).Trim();
                }
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
            get { return "git status"; }
        }

        protected override string ProcessArguments
        {
            get { return "status -b --porcelain"; }
        }
    }
}
