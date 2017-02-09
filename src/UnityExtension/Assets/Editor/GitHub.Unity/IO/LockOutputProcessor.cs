using System;

namespace GitHub.Unity
{
    class LockOutputProcessor : BaseOutputProcessor
    {
        private IGitStatusEntryFactory gitStatusEntryFactory;

        public LockOutputProcessor(IGitStatusEntryFactory gitStatusEntryFactory)
        {
            this.gitStatusEntryFactory = gitStatusEntryFactory;
        }

        public override void LineReceived(string line)
        {
            base.LineReceived(line);

            if (line == null || OnGitLock == null)
            {
                return;
            }

            if (line.EndsWith("lock(s) matched query."))
            {
                return;
            }

            var proc = new LineParser(line);
            if (proc.IsAtEnd)
            {
                return;
            }

            string file;
            if (proc.Matches('"'))
            {
                proc.MoveNext();
                file = proc.ReadUntil('"');
            }
            else
            {
                file = proc.ReadUntilWhitespace();
            }

            var server = proc.ReadUntilWhitespace();
            var user = proc.ReadUntilWhitespace();
            var userId = Convert.ToInt32(proc.ReadUntilWhitespace());

            var gitLock = gitStatusEntryFactory.CreateGitLock(file, server, user, userId);
            OnGitLock.SafeInvoke(gitLock);
        }

        public event Action<GitLock> OnGitLock;
    }
}
