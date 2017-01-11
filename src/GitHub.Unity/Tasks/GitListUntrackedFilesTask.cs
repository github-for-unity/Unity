using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHub.Unity
{
    class GitListUntrackedFilesTask : GitTask
    {
        private List<GitStatusEntry> entries = new List<GitStatusEntry>();
        private Action onFailure;
        private Action<GitListUntrackedFilesTask> onSuccess;

        private GitListUntrackedFilesTask(Action<GitListUntrackedFilesTask> success, Action failure = null)
        {
            onSuccess = success;
            onFailure = failure;
        }

        public static void Schedule(Action<GitListUntrackedFilesTask> success, Action failure = null)
        {
            Tasks.Add(new GitListUntrackedFilesTask(success, failure));
        }

        protected override void OnProcessOutputUpdate()
        {
            var buffer = OutputBuffer.GetStringBuilder();
            var end = buffer.Length - 1;

            // Only try to avoid partial lines if the process did not already end
            if (!Done)
            {
                for (; end > 0 && buffer[end] != '\n'; --end) ;
            }

            // Parse output lines into the entries list if we have any buffer to parse
            if (end > 0)
            {
                for (int index = 0, last = -1; index <= end; ++index)
                {
                    if (buffer[index] == '\n')
                    {
                        ParseOutputLine(last + 1, index);
                        last = index;
                    }
                }

                buffer.Remove(0, end + 1);
            }

            // Process results when we are done
            if (Done)
            {
                buffer = ErrorBuffer.GetStringBuilder();

                // We failed. Make a little noise.
                if (buffer.Length > 0)
                {
                    Tasks.ReportFailure(FailureSeverity.Moderate, this, buffer.ToString());
                    if (onFailure != null)
                    {
                        Tasks.ScheduleMainThread(() => onFailure());
                    }
                }
                // We succeeded. Hand over the results!
                else if (onSuccess != null)
                {
                    Tasks.ScheduleMainThread(() => onSuccess(this));
                }
            }
        }

        private void ParseOutputLine(int start, int end)
        {
            var path = OutputBuffer.GetStringBuilder().ToString(start, end - start);
            if (!entries.Any(e => e.Path.Equals(path)))
            {
                entries.Add(new GitStatusEntry(path, GitFileStatus.Untracked));
            }
        }

        public override bool Blocking
        {
            get { return false; }
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
            get { return "git ls-files"; }
        }

        public IList<GitStatusEntry> Entries
        {
            get { return entries; }
        }

        protected override string ProcessArguments
        {
            get { return "ls-files -o --exclude-standard"; }
        }
    }
}
