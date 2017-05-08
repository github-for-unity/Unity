using System.Threading;

namespace GitHub.Unity
{
    class FindGitTask : ProcessTask<string>
    {
        private readonly IEnvironment environment;

        public FindGitTask(IEnvironment environment,
            CancellationToken token, IOutputProcessor<string> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new FirstNonNullLineOutputProcessor(), dependsOn)
        {
            this.environment = environment;
        }

        public override string Name { get { return "find git"; } }
        public override string ProcessName { get { return environment.IsWindows ? "where" : "which"; } }
        public override string ProcessArguments { get { return "git"; } }
    }
}
