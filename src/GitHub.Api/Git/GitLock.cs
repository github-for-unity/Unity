using System;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitLock
    {
        public static GitLock Default = new GitLock();

        public int id;
        public string path;
        public GitUser owner;
        public DateTimeOffset locked_at;

        [NotSerialized] public int ID => id;
        [NotSerialized] public NPath Path => path.ToNPath();
        [NotSerialized] public GitUser Owner => owner;
        [NotSerialized] public DateTimeOffset LockedAt => locked_at;

        public GitLock(int id, NPath path, GitUser owner, DateTimeOffset locked_at)
        {
            this.id = id;
            this.path = path;
            this.owner = owner;
            this.locked_at = locked_at;
        }

        public override bool Equals(object other)
        {
            if (other is GitLock)
                return Equals((GitLock)other);
            return false;
        }

        public bool Equals(GitLock other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + id.GetHashCode();
            hash = hash * 23 + Path.GetHashCode();
            hash = hash * 23 + owner.GetHashCode();
            hash = hash * 23 + locked_at.GetHashCode();
            return hash;
        }

        public static bool operator ==(GitLock lhs, GitLock rhs)
        {
            return lhs.id == rhs.id && lhs.Path == rhs.Path && lhs.owner == rhs.owner && lhs.locked_at == rhs.locked_at;
        }

        public static bool operator !=(GitLock lhs, GitLock rhs)
        {
            return !(lhs == rhs);
        }
        public override string ToString()
        {
            return $"{{id:{id}, path:{Path}, owner:{{{owner}}}, locked_at:'{locked_at}'}}";
        }
    }
}
