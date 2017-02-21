using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitListLocalBranchesTask : GitTask
    {
        private const string Arguments = "branch -vv";

        private readonly ITaskResultDispatcher<IEnumerable<GitBranch>> resultDispatcher;
        private readonly List<GitBranch> branches = new List<GitBranch>();
        private readonly BranchListOutputProcessor processor = new BranchListOutputProcessor();

        public GitListLocalBranchesTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<IEnumerable<GitBranch>> resultDispatcher)
            : base(environment, processManager)
        {
            this.resultDispatcher = resultDispatcher;
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnBranch += AddBranch;
            return new ProcessOutputManager(process, processor);
        }

        protected override void RaiseOnSuccess()
        {
            resultDispatcher.ReportSuccess(branches);
        }

        private void AddBranch(GitBranch branch)
        {
            branches.Add(branch);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }

        public override bool Cached { get { return false; } }

        public override string Label { get { return "git list branch"; } }

        protected override string ProcessArguments
        {
            get { return Arguments ; }
        }
    }


    class GitListRemoteBranchesTask : GitTask
    {
        private const string Arguments = "branch -vvr";

        private readonly ITaskResultDispatcher<IEnumerable<GitBranch>> resultDispatcher;
        private readonly List<GitBranch> branches = new List<GitBranch>();
        private readonly BranchListOutputProcessor processor = new BranchListOutputProcessor();

        public GitListRemoteBranchesTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<IEnumerable<GitBranch>> resultDispatcher)
            : base(environment, processManager)
        {
            this.resultDispatcher = resultDispatcher;
        }

        protected override ProcessOutputManager HookupOutput(IProcess process)
        {
            processor.OnBranch += AddBranch;
            return new ProcessOutputManager(process, processor);
        }

        protected override void RaiseOnSuccess()
        {
            resultDispatcher.ReportSuccess(branches);
        }

        private void AddBranch(GitBranch branch)
        {
            branches.Add(branch);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }

        public override bool Cached { get { return false; } }

        public override string Label { get { return "git list branch"; } }

        protected override string ProcessArguments
        {
            get { return Arguments; }
        }
    }
}
