using System.Threading;

namespace GitHub.Unity
{
    class GitStatusTask : ProcessTask<GitStatus?>
    {
        public GitStatusTask(IGitObjectFactory gitObjectFactory,
            CancellationToken token, IOutputProcessor<GitStatus?> processor = null)
            : base(token, processor ?? new StatusOutputProcessor(gitObjectFactory))
        {
        }

        public override string Name { get { return "git status"; } }

        public override string ProcessArguments
        {
            get { return "-c i18n.logoutputencoding=utf8 -c core.quotepath=false status -b -u --ignored --porcelain"; }
        }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}
