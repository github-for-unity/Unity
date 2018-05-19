using System.Threading;

namespace GitHub.Unity
{
    class GitLogTask : ProcessTaskWithListOutput<GitLogEntry>
    {
        private const string TaskName = "git log";

        public GitLogTask(IGitObjectFactory gitObjectFactory,
            CancellationToken token, BaseOutputListProcessor<GitLogEntry> processor = null)
            : base(token, processor ?? new LogEntryOutputProcessor(gitObjectFactory))
        {
            Name = TaskName;
        }

        public override string ProcessArguments
        {
            get { return @"-c i18n.logoutputencoding=utf8 -c core.quotepath=false log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status"; }
        }
        public override string Message { get; set; } = "Loading the history...";
    }
}
