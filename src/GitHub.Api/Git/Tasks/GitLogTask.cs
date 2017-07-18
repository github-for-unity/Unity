using System.Threading;

namespace GitHub.Unity
{
    class GitLogTask : ProcessTaskWithListOutput<GitLogEntry>
    {
        public GitLogTask(IGitObjectFactory gitObjectFactory,
            CancellationToken token, BaseOutputListProcessor<GitLogEntry> processor = null)
            : base(token, processor ?? new LogEntryOutputProcessor(gitObjectFactory))
        {
        }

        public override string Name { get { return "git log"; } }

        public override string ProcessArguments
        {
            get { return @"-c i18n.logoutputencoding=utf8 -c core.quotepath=false log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status"; }
        }
    }
}
