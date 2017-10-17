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
        public string Name;
        public string Url;
        public string Login;
        public string User;
        public string Token;
        public string Host;
        public GitRemoteFunction Function;

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