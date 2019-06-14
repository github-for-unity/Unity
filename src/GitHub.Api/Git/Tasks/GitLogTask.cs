using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitLogTask : ProcessTaskWithListOutput<GitLogEntry>
    {
        private const string TaskName = "git log";
        private const string baseArguments = @"-c i18n.logoutputencoding=utf8 -c core.quotepath=false log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status";
        private readonly string arguments;

        public GitLogTask(IGitObjectFactory gitObjectFactory,
            CancellationToken token,
            BaseOutputListProcessor<GitLogEntry> processor = null)
            : this(0, gitObjectFactory, token, processor)
        {
        }

        public GitLogTask(string file,
            IGitObjectFactory gitObjectFactory,
            CancellationToken token, BaseOutputListProcessor<GitLogEntry> processor = null)
            : this(file, 0, gitObjectFactory, token, processor)
        {
        }

        public GitLogTask(int numberOfCommits, IGitObjectFactory gitObjectFactory,
            CancellationToken token,
            BaseOutputListProcessor<GitLogEntry> processor = null)
            : base(token, processor ?? new LogEntryOutputProcessor(gitObjectFactory))
        {
            Name = TaskName;
            arguments = baseArguments;
            if (numberOfCommits > 0)
                arguments += " -n " + numberOfCommits;
        }

        public GitLogTask(string file, int numberOfCommits,
            IGitObjectFactory gitObjectFactory,
            CancellationToken token, BaseOutputListProcessor<GitLogEntry> processor = null)
            : base(token, processor ?? new LogEntryOutputProcessor(gitObjectFactory))
        {
            Name = TaskName;
            arguments = baseArguments;
            if (numberOfCommits > 0)
                arguments += " -n " + numberOfCommits;
            arguments += " -- ";
            arguments += " \"" + file + "\"";
        }

        public override string ProcessArguments
        {
            get { return arguments; }
        }
        public override string Message { get; set; } = "Loading the history...";
    }
}
