using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitAddTask : GitTask
    {
        private readonly string arguments;

        public GitAddTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            IEnumerable<string> files)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNull(files, "files");

            arguments = "add ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToNPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            var processor = new BaseOutputProcessor();
            processor.OnData += OutputBuffer.WriteLine;
            process.OnErrorData += Process_OnErrorData;
            return new ProcessOutputManager(process, processor);
        }

        private void Process_OnErrorData(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            if (line.StartsWith("fatal:"))
                ErrorBuffer.WriteLine(line);
        }

        public override bool Blocking { get { return false; } }
        public override string Label { get { return "git add"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
