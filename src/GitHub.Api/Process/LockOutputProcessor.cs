using System;

namespace GitHub.Unity
{
    class LockOutputProcessor : BaseOutputProcessor
    {
        private IGitObjectFactory gitObjectFactory;

        public LockOutputProcessor(IGitObjectFactory gitObjectFactory)
        {
            this.gitObjectFactory = gitObjectFactory;
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

            proc.SkipWhitespace();
            var user = proc.ReadUntil('<').TrimEnd();

            var gitLock = gitObjectFactory.CreateGitLock(file, user);
            OnGitLock?.Invoke(gitLock);
        }

        public event Action<GitLock> OnGitLock;
    }
}
