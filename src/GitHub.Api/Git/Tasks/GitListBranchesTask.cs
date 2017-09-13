using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitListLocalBranchesTask : ProcessTaskWithListOutput<GitBranch>
    {
        private const string TaskName = "git list local branches";
        private const string Arguments = "branch -vv";

        public GitListLocalBranchesTask(CancellationToken token, IOutputProcessor<GitBranch, List<GitBranch>> processor = null)
            : base(token, processor ?? new BranchListOutputProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments
        {
            get { return Arguments ; }
        }
    }


    class GitListRemoteBranchesTask : ProcessTaskWithListOutput<GitBranch>
    {
        private const string TaskName = "git list remote branches";
        private const string Arguments = "branch -vvr";

        public GitListRemoteBranchesTask(CancellationToken token)
            : base(token, new BranchListOutputProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments
        {
            get { return Arguments; }
        }
    }
}
