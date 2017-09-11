using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    enum CommitState
    {
        None,
        Some,
        All
    }

    class FileTreeNode
    {
        private List<FileTreeNode> children;
        private string path;
        private CommitState state;

        public object Icon;
        public string Label;
        public bool Open = true;
        public string RepositoryPath;
        public GitCommitTarget Target { get; set; }

        public FileTreeNode()
        {
            children = new List<FileTreeNode>();
        }

        public FileTreeNode(string path)
        {
            this.path = path ?? "";
            Label = this.path;
            children = new List<FileTreeNode>();
        }

        public FileTreeNode Add(FileTreeNode child)
        {
            children.Add(child);
            return child;
        }

        public CommitState State
        {
            get
            {
                if (children == null)
                    return state;

                var commitState = CommitState.None;
                if (Target != null)
                {
                    commitState = Target.All ? CommitState.All : Target.Any ? CommitState.Some : CommitState.None;
                    if (!children.Any())
                    {
                        state = commitState;
                        return state;
                    }
                }

                var allCount = children.Count(c => c.State == CommitState.All);

                if (allCount == children.Count && (commitState == CommitState.All || Target == null))
                {
                    state = CommitState.All;
                    return state;
                }

                if (allCount > 0 || commitState == CommitState.Some)
                {
                    state = CommitState.Some;
                    return state;
                }

                var someCount = children.Count(c => c.State == CommitState.Some);
                if (someCount > 0 || commitState == CommitState.Some)
                {
                    state = CommitState.Some;
                    return state;
                }
                state = CommitState.None;
                return state;
            }

            set
            {
                if (value == state)
                {
                    return;
                }

                if (Target != null)
                {
                    if (value == CommitState.None)
                    {
                        Target.Clear();
                    }
                    else if (value == CommitState.All)
                    {
                        Target.All = true;
                    }
                }

                state = value;

                if (children == null)
                {
                    return;
                }

                for (var index = 0; index < children.Count; ++index)
                {
                    children[index].State = value;
                }
            }
        }

        public string Path
        {
            get { return path; }
        }

        public IEnumerable<FileTreeNode> Children
        {
            get {
                if (children == null)
                    children = new List<FileTreeNode>();
                return children;
            }
        }

        private ILogging logger;
        protected ILogging Logger
        {
            get
            {
                if (logger == null)
                    logger = Logging.GetLogger(GetType());
                return logger;
            }
        }
    }
}