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