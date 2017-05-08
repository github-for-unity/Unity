using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitConfigListTaskTask : ProcessTaskWithListOutput<KeyValuePair<string, string>>
    {
        private readonly static ConfigOutputProcessor defaultProcessor = new ConfigOutputProcessor();
        public GitConfigListTaskTask(CancellationToken token, ConfigOutputProcessor processor = null)
            : base(token, processor ?? defaultProcessor)
        {
            this.Name = "git config";
        }

        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}