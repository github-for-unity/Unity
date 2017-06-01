using System.Threading;

namespace GitHub.Unity
{
    class GitListLocalBranchesTask : ProcessTaskWithListOutput<GitBranch>
    {
        private const string Arguments = "branch -vv";

        public GitListLocalBranchesTask(CancellationToken token, BaseOutputListProcessor<GitBranch> processor = null, ITask dependsOn = null)
            : base(token, processor ?? new BranchListOutputProcessor(), dependsOn)
        {
        }

        public override string Name { get { return "git list branch"; } }

        public override string ProcessArguments
        {
            get { return Arguments ; }
        }
    }


    class GitListRemoteBranchesTask : ProcessTaskWithListOutput<GitBranch>
    {
        private const string Arguments = "branch -vvr";

        public GitListRemoteBranchesTask(CancellationToken token)
            : base(token, new BranchListOutputProcessor())
        {
        }

        public override string Name { get { return "git list branch"; } }

        public override string ProcessArguments
        {
            get { return Arguments; }
        }
    }
}
