using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitFileLog
    {
        public static GitFileLog Default = new GitFileLog(null, new List<GitLogEntry>(0));

        public string path;
        public List<GitLogEntry> logEntries;

        public GitFileLog(string path, List<GitLogEntry> logEntries)
        {
            this.path = path;
            this.logEntries = logEntries;
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public List<GitLogEntry> LogEntries
        {
            get { return logEntries; }
            set { logEntries = value; }
        }
    }
}
