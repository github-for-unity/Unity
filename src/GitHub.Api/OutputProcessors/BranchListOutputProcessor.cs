using System;
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
                proc.Matches('*');
                proc.SkipWhitespace();
                var detached = proc.Matches("(HEAD ");
                var name = "detached";
                if (detached)
                {
                    proc.MoveToAfter(')');
                }
                else
                {
                    name = proc.ReadUntilWhitespace();
                }
                proc.SkipWhitespace();
                proc.ReadUntilWhitespace();
                var tracking = proc.Matches(trackingBranchRegex);
                var trackingName = "";
                if (tracking)
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
