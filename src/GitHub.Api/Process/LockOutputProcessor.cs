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
            Logger.Debug(line);
            var path = proc.ReadUntil('\t').Trim();
            var user = proc.ReadUntilLast("ID:").Trim();
            proc.MoveToAfter("ID:");
            var id = int.Parse(proc.ReadToEnd().Trim());

            OnGitLock(gitObjectFactory.CreateGitLock(path, user, id));
        }

        public event Action<GitLock> OnGitLock;
    }
}
