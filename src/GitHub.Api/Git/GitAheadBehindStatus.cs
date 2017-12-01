using System;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitAheadBehindStatus
    {
        public static GitAheadBehindStatus Default = new GitAheadBehindStatus();

        public int ahead;
        public int behind;

        public GitAheadBehindStatus(int ahead, int behind)
        {
            this.ahead = ahead;
            this.behind = behind;
        }

        public int Ahead => ahead;

        public int Behind => behind;
    }
}