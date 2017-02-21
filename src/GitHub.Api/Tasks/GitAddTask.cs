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
                arguments += " " + file;
            }
        }
        
        public override bool Blocking { get { return false; } }
        public override string Label { get { return "git add"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
