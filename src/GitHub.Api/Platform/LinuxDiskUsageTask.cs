using System.Threading;

namespace GitHub.Unity
{
    class LinuxDiskUsageTask : ProcessTask<int>
    {
        private readonly string arguments;

        public LinuxDiskUsageTask(NPath directory, CancellationToken token)
            : base(token, new LinuxDiskUsageOutputProcessor())
        {
            Name = "du";
            arguments = string.Format("-h \"{0}\"", directory);
        }

        public override string ProcessName { get { return Name; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
        public override string Message { get; set; } = "Getting directory size...";
    }
}