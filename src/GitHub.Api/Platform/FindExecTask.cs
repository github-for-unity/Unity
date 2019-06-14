using System.Threading;

namespace GitHub.Unity
{
    public class FindExecTask : ProcessTask<NPath>
    {
        private readonly string arguments;

        public FindExecTask(string executable, CancellationToken token)
            : base(token, new FirstLineIsPathOutputProcessor())
        {
            Name = DefaultEnvironment.OnWindows ? "where" : "which";
            arguments = executable;
        }

        public override string ProcessName { get { return Name; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}
