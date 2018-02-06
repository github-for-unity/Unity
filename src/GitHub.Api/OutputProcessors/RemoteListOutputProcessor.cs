using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    class RemoteListOutputProcessor : BaseOutputListProcessor<GitRemote>
    {
        private string currentName;
        private string currentUrl;
        private List<string> currentModes;

        public RemoteListOutputProcessor()
        {
            Reset();
        }

        public override void LineReceived(string line)
        {
            //origin https://github.com/github/VisualStudio.git (fetch)

            if (line == null)
            {
                ReturnRemote();
                return;
            }

            var proc = new LineParser(line);
            var name = proc.ReadUntilWhitespace();
            proc.SkipWhitespace();

            var url = proc.ReadUntilWhitespace();
            proc.SkipWhitespace();

            proc.MoveNext();
            var mode = proc.ReadUntil(')');

            if (currentName == null)
            {
                currentName = name;
                currentUrl = url;
                currentModes.Add(mode);
            }
            else if (currentName == name)
            {
                currentModes.Add(mode);
            }
            else
            {
                ReturnRemote();

                currentName = name;
                currentUrl = url;
                currentModes.Add(mode);
            }
        }

        private void ReturnRemote()
        {
            var modes = currentModes.Select(s => s.ToUpperInvariant()).ToArray();

            var isFetch = modes.Contains("FETCH");
            var isPush = modes.Contains("PUSH");

            GitRemoteFunction remoteFunction;
            if (isFetch && isPush)
            {
                remoteFunction = GitRemoteFunction.Both;
            }
            else if (isFetch)
            {
                remoteFunction = GitRemoteFunction.Fetch;
            }
            else if (isPush)
            {
                remoteFunction = GitRemoteFunction.Push;
            }
            else
            {
                remoteFunction = GitRemoteFunction.Unknown;
            }

            string host;
            string user = null;
            var proc = new LineParser(currentUrl);
            if (proc.Matches("http") || proc.Matches("https"))
            {
                proc.MoveToAfter(':');
                proc.MoveNext();
                proc.MoveNext();
                host = proc.ReadUntil('/');
            }
            else
            {
                //Assuming SSH here
                user = proc.ReadUntil('@');
                proc.MoveNext();
                host = proc.ReadUntil(':');

                currentUrl = currentUrl.Substring(user.Length + 1);
            }

            RaiseOnEntry(new GitRemote(currentName, host, currentUrl, remoteFunction, user, null, null));
            Reset();
        }

        private void Reset()
        {
            currentName = null;
            currentModes = new List<string>();
            currentUrl = null;
        }
    }
}