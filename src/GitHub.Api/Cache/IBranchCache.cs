using System.Collections.Generic;

namespace GitHub.Unity
{
    public interface IBranchCache
    {
        List<GitBranch> LocalBranches { get; set; }
        List<GitBranch> RemoteBranches { get; set; }
    }
}
