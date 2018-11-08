using System.Threading;

namespace GitHub.Unity
{
    class GitLogTask : ProcessTaskWithListOutput<GitLogEntry>
    {
        private const string TaskName = "git log";
        private const string baseArguments = @"-c i18n.logoutputencoding=utf8 -c core.quotepath=false log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status";
        private readonly string arguments;

        public GitLogTask(IGitObjectFactory gitObjectFactory,
            CancellationToken token, BaseOutputListProcessor<GitLogEntry> processor = null)
            : base(token, processor ?? new LogEntryOutputProcessor(gitObjectFactory))
        {
            Name = TaskName;
            arguments = baseArguments;
        }

        public GitLogTask(NPath file,
            IGitObjectFactory gitObjectFactory,
            CancellationToken token, BaseOutputListProcessor<GitLogEntry> processor = null)
            : base(token, processor ?? new LogEntryOutputProcessor(gitObjectFactory))
        {
            Name = TaskName;
            arguments = baseArguments;
            arguments += " -- ";
            arguments += " \"" + file.ToString(SlashMode.Forward) + "\"";
        }

        public override string ProcessArguments
        {
            get { return arguments; }
        }
        public override string Message { get; set; } = "Loading the history...";
    }
}
