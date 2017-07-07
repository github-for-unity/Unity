using System.Threading;

namespace GitHub.Unity
{
    class FindExecTask : ProcessTask<NPath>
    {
        private readonly string arguments;
        private readonly string name;

        public FindExecTask(string executable, CancellationToken token)
            : base(token, new FirstLineIsPathOutputProcessor())
        {
            Name = name = DefaultEnvironment.OnWindows ? "where" : "which";
            arguments = executable;
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}
