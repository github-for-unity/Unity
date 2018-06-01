using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class BranchListOutputProcessor : BaseOutputListProcessor<GitBranch>
    {
        private static readonly Regex trackingBranchRegex = new Regex(@"\[[\w]+\/.*\]");

        public override bool LineReceived(string line)
        {
            if (line == null)
                return false;

            var proc = new LineParser(line);
            if (proc.IsAtEnd)
                return false;

            var active = proc.Matches('*');
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
            }

            var branch = new GitBranch(name, trackingName);

            RaiseOnEntry(branch);
            return false;
        }
    }
}