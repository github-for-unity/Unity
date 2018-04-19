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

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + ahead.GetHashCode();
            hash = hash * 23 + behind.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitAheadBehindStatus)
                return Equals((GitAheadBehindStatus)other);
            return false;
        }

        public bool Equals(GitAheadBehindStatus other)
        {
            return ahead == other.ahead && behind == other.behind;
        }

        public static bool operator ==(GitAheadBehindStatus lhs, GitAheadBehindStatus rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GitAheadBehindStatus lhs, GitAheadBehindStatus rhs)
        {
            return !(lhs == rhs);
        }

        public int Ahead => ahead;

        public int Behind => behind;
    }
}