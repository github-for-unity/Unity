using System;
using System.Text;

namespace GitHub.Unity
{
    struct GitRemote
    {
        public string Name;
        public string URL;
        public string Login;
        public string User;
        public string Token;
        public string Host;
        public GitRemoteFunction Function;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(String.Format("Name: {0}", Name));
            sb.AppendLine(String.Format("URL: {0}", URL));
            sb.AppendLine(String.Format("Login: {0}", Login));
            sb.AppendLine(String.Format("User: {0}", User));
            sb.AppendLine(String.Format("Token: {0}", Token));
            sb.AppendLine(String.Format("Host: {0}", Host));
            sb.AppendLine(String.Format("Function: {0}", Function));
            return sb.ToString();
        }
    }
}