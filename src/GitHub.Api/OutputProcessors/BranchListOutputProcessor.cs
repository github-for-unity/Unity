using System;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class BranchListOutputProcessor : BaseOutputListProcessor<GitBranch>
    {
        private static readonly Regex trackingBranchRegex = new Regex(@"\[[\w]+\/.*\]");

        public override void LineReceived(string line)
        {
            if (line == null)
                return;

            var proc = new LineParser(line);
            if (proc.IsAtEnd)
                return;

            try
            {
                string name;
                string trackingName = null;

                if (proc.Matches('*'))
                    proc.MoveNext();
                proc.SkipWhitespace();
                if (proc.Matches("(HEAD "))
                {
                    name = "detached";
                    proc.MoveToAfter(')');
                }
                else
                {
                    name = proc.ReadUntilWhitespace();
                }

                proc.ReadUntilWhitespaceTrim();
                if (proc.Matches(trackingBranchRegex))
                {
                    trackingName = proc.ReadChunk('[', ']');
                    var indexOf = trackingName.IndexOf(':');
                    if (indexOf != -1)
                    {
                        trackingName = trackingName.Substring(0, indexOf);
                    }
                }

                var branch = new GitBranch(name, trackingName);
                RaiseOnEntry(branch);
            }
            catch(Exception ex)
            {
                Logger.Warning(ex, "Unexpected input when listing branches");
            }
        }
    }
}
