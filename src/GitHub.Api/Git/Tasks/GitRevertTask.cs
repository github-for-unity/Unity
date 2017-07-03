using System.Threading;

namespace GitHub.Unity
{
    class GitRevertTask : ProcessTask<string>
    {
        private readonly string arguments;

        public GitRevertTask(string changeset,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNull(changeset, "changeset");

            arguments = $"revert --no-edit {changeset}";
        }

        public override string Name { get { return "git revert"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}