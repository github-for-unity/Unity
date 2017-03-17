using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class LockOutputProcessor : BaseOutputProcessor
    {
        private static readonly Regex locksSummaryLineRegex = new Regex(@".*?lock\s?\(s\) matched query.",
            RegexOptions.Compiled);

        private IGitObjectFactory gitObjectFactory;

        public LockOutputProcessor(IGitObjectFactory gitObjectFactory)
        {
            this.gitObjectFactory = gitObjectFactory;
        }

        public override void LineReceived(string line)
        {
            base.LineReceived(line);

            if (OnGitLock == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(line))
            {
                //Do Nothing
                return;
            }

            var proc = new LineParser(line);
            if (proc.Matches(locksSummaryLineRegex))
            {
                return;
            }

            var path = proc.ReadUntil('\t').Trim();
            proc.MoveNext();

            var user = proc.ReadToEnd();

            OnGitLock(gitObjectFactory.CreateGitLock(path, user));
        }

        public event Action<GitLock> OnGitLock;
    }
}
