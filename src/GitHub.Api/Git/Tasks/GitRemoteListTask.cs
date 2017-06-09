using System.Threading;

namespace GitHub.Unity
{
    class GitRemoteListTask : ProcessTaskWithListOutput<GitRemote>
    {
        public GitRemoteListTask(CancellationToken token, BaseOutputListProcessor<GitRemote> processor = null)
            : base(token, processor ?? new RemoteListOutputProcessor())
        {
        }

        public override string Name { get { return "git remote"; } }
        public override string ProcessArguments { get { return "remote -v"; } }
    }
}
