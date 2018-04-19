using System;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitLock
    {
        public static GitLock Default = new GitLock();

        public int ID;
        public string Path;
        public string FullPath;
        public string User;

        public GitLock(string path, string fullPath, string user, int id)
        {
            Path = path;
            FullPath = fullPath;
            User = user;
            ID = id;
        }

        public override bool Equals(object other)
        {
            if (other is GitLock)
            {
                return this.Equals((GitLock)other);
            }
            return false;
        }

        public bool Equals(GitLock p)
        {
            return ID == p.ID;
        }

        public override int GetHashCode()
        {
            return 17 * 23 + ID.GetHashCode();
        }

        public static bool operator ==(GitLock lhs, GitLock rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GitLock lhs, GitLock rhs)
        {
            return !(lhs.Equals(rhs));
        }
        public override string ToString()
        {
            return $"{{GitLock ({User}) '{Path}'}}";
        }
    }
}
