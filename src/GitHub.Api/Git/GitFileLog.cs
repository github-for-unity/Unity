using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitFileLog
    {
        public static GitFileLog Default = new GitFileLog(NPath.Default, new List<GitLogEntry>(0));

        public NPath path;
        public List<GitLogEntry> logEntries;

        public GitFileLog(NPath path, List<GitLogEntry> logEntries)
        {
            this.path = path;
            this.logEntries = logEntries;
        }

        public NPath Path
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
