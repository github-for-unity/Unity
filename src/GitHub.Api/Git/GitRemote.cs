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
        public static GitRemote Default = new GitRemote();

        public string name;
        public string url;
        public string login;
        public string user;
        public string host;
        public GitRemoteFunction function;
        public readonly string token;

        public string Name
        {
            get { return name; }
        }

        public string Url
        {
            get { return url; }
        }

        public string Login
        {
            get { return login; }
        }

        public string User
        {
            get { return user; }
        }

        public string Token
        {
            get { return token; }
        }

        public string Host
        {
            get { return host; }
        }

        public GitRemoteFunction Function
        {
            get { return function; }
        }

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
            login = null;
            token = null;
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
            login = null;
            user = null;
            token = null;
            host = null;
            function = GitRemoteFunction.Unknown;
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
    }
}