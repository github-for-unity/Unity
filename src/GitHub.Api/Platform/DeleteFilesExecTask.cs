using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class DeleteFilesExecTask : ProcessTask<List<string>>
    {
        private readonly string arguments;

        public DeleteFilesExecTask(string[] files, CancellationToken token)
            : base(token, new SimpleListOutputProcessor())
        {
            Name = DefaultEnvironment.OnWindows ? "cmd" : "rm";

            var fileString = string.Join(" ", files);
            if (DefaultEnvironment.OnWindows)
            {
                arguments = $"/c \"del {fileString}\"";
            }
            else
            {
                arguments = fileString;
            }

        }

        public override string ProcessName { get { return Name; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}