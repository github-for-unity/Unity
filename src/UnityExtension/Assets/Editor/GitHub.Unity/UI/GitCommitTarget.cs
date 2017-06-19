using System;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class GitCommitTarget
    {
        [SerializeField] public bool All = false;

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