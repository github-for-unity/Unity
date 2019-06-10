using System.Threading;

namespace GitHub.Unity.Git.Tasks
{
    public class GitListLocalBranchesTask : ProcessTaskWithListOutput<GitBranch>
    {
        private const string TaskName = "git list local branches";
        private const string Arguments = "branch -vv";

        public GitListLocalBranchesTask(CancellationToken token, BaseOutputListProcessor<GitBranch> processor = null)
            : base(token, processor ?? new BranchListOutputProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments => Arguments;
        public override string Message { get; set; } = "Listing local branches...";
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

        public override string ProcessArguments => Arguments;
        public override string Message { get; set; } = "Listing remote branches...";
    }
}
