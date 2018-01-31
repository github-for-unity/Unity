using System;
using System.Text;

namespace GitHub.Unity
{
    public enum GitRemoteFunction
    {
        Unknown,
        Fetch,
        Push,
        Both
    }

    [Serializable]
    public struct GitRemote
    {
        public static GitRemote Default = new GitRemote(String.Empty, String.Empty, String.Empty, GitRemoteFunction.Unknown, string.Empty, string.Empty, string.Empty);

        public string name;
        public string url;
        public string login;
        public string user;
        public string host;
        public GitRemoteFunction function;
        public string token;

        public GitRemote(string name, string host, string url, GitRemoteFunction function, string user, string login, string token)
        {
            this.name = name;
            this.url = url;
            this.host = host;
            this.function = function;
            this.user = user;
            this.login = login;
            this.token = token;
        }

        public GitRemote(string name, string host, string url, GitRemoteFunction function, string user)
        {
            this.name = name;
            this.url = url;
            this.host = host;
            this.function = function;
            this.user = user;
            this.login = null;
            this.token = null;
        }

        public GitRemote(string name, string host, string url, GitRemoteFunction function)
        {
            this.name = name;
            this.url = url;
            this.host = host;
            this.function = function;
            this.user = null;
            this.login = null;
            this.token = null;
        }

        public GitRemote(string name, string url)
        {
            this.name = name;
            this.url = url;
            this.login = null;
            this.user = null;
            this.token = null;
            this.host = null;
            this.function = GitRemoteFunction.Unknown;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (name?.GetHashCode() ?? 0);
            hash = hash * 23 + (url?.GetHashCode() ?? 0);
            hash = hash * 23 + (login?.GetHashCode() ?? 0);
            hash = hash * 23 + (user?.GetHashCode() ?? 0);
            hash = hash * 23 + (host?.GetHashCode() ?? 0);
            hash = hash * 23 + function.GetHashCode();
            hash = hash * 23 + (token?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitRemote)
                return Equals((GitRemote)other);
            return false;
        }

        public bool Equals(GitRemote other)
        {
            return
                String.Equals(name, other.name) &&
                String.Equals(url, other.url) &&
                String.Equals(login, other.login) &&
                String.Equals(user, other.user) &&
                String.Equals(host, other.host) &&
                function == other.function &&
                String.Equals(token, other.token)
                ;
        }

        public static bool operator ==(GitRemote lhs, GitRemote rhs)
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

        public static bool operator !=(GitRemote lhs, GitRemote rhs)
        {
            return !(lhs == rhs);
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(String.Format("Name: {0}", Name));
            sb.AppendLine(String.Format("URL: {0}", Url));
            sb.AppendLine(String.Format("Login: {0}", Login));
            sb.AppendLine(String.Format("User: {0}", User));
            sb.AppendLine(String.Format("Host: {0}", Host));
            sb.AppendLine(String.Format("Function: {0}", Function));
            return sb.ToString();
        }

        public string Name => name;
        public string Url => url;
        public string Login => login;
        public string User => user;
        public string Token => token;
        public string Host => host;
        public GitRemoteFunction Function => function;
    }
}