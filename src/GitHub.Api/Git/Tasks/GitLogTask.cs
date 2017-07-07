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
            get { return @"log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status"; }
        }
    }
}
