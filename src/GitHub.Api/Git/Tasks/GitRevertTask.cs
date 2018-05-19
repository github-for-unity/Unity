using System.Threading;

namespace GitHub.Unity
{
    class GitRevertTask : ProcessTask<string>
    {
        private const string TaskName = "git revert";
        private readonly string arguments;

        public GitRevertTask(string changeset,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNull(changeset, "changeset");
            Name = TaskName;
            arguments = $"revert --no-edit {changeset}";
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Reverting commit...";
    }
}