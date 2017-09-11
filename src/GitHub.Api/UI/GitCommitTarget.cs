using System;

namespace GitHub.Unity
{
    [Serializable]
    class GitCommitTarget
    {
        public bool All = false;

        public void Clear()
        {
            All = false;
        }

        public bool Any
        {
            get
            {
                return All;
            }
        }
    }
}