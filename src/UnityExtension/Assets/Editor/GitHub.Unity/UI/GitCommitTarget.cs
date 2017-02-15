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
            // TODO: Add line tracking here
        }

        // TODO: Add line tracking here

        public bool Any
        {
            get
            {
                return All; // TODO: Add line tracking here
            }
        }
    }
}