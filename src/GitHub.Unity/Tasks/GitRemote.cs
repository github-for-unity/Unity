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

        public static bool TryParse(string line, out GitRemote result)
        {
            var match = Utility.ListRemotesRegex.Match(line);

            if (!match.Success)
            {
                result = new GitRemote();
                return false;
            }

            result = new GitRemote() {
                Name = match.Groups["name"].Value,
                URL = match.Groups["url"].Value,
                Login = match.Groups["login"].Value,
                User = match.Groups["user"].Value,
                Token = match.Groups["token"].Value,
                Host = match.Groups["host"].Value
            };

            try
            {
                result.Function = (GitRemoteFunction)Enum.Parse(typeof(GitRemoteFunction), match.Groups["function"].Value, true);
            }
            catch (Exception)
            {}

            return true;
        }

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