using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class LockOutputProcessor : BaseOutputListProcessor<GitLock>
    {
        private static readonly Regex locksSummaryLineRegex = new Regex(@".*?lock\s?\(s\) matched query.",
            RegexOptions.Compiled);

        private IGitObjectFactory gitObjectFactory;

        public LockOutputProcessor(IGitObjectFactory gitObjectFactory)
        {
            Guard.ArgumentNotNull(gitObjectFactory, "gitObjectFactory");
            this.gitObjectFactory = gitObjectFactory;
        }

        public override void LineReceived(string line)
        {
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
            var user = proc.ReadUntilLast("ID:").Trim();
            proc.MoveToAfter("ID:");
            var id = int.Parse(proc.ReadToEnd().Trim());

            RaiseOnEntry(gitObjectFactory.CreateGitLock(path, user, id));
        }
    }
}
