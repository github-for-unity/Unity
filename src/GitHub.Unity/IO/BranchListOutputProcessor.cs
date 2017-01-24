using System;
using System.Text.RegularExpressions;
using GitHub.Unity.Logging;

namespace GitHub.Unity
{
    class BranchListOutputProcessor : BaseOutputProcessor
    {
        private static readonly Regex TrackingBranchRegex = new Regex(@"\[[\w]+\/.*\]");

        public event Action<GitBranch> OnBranch;

        static readonly ILogger Logger = Logging.Logger.GetLogger<BranchListOutputProcessor>();

        public override void LineReceived(string line)
        {
            base.LineReceived(line);

            if (line == null || OnBranch == null)
                return;

            var proc = new LineParser(line);
            if (proc.IsAtEnd)
                return;

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
            var tracking = proc.Matches(TrackingBranchRegex);
            var trackingName = "";
            if (tracking)
            {
                trackingName = proc.ReadChunk('[', ']');
            }

            Logger.Debug("Branch - Name: " + name + " TrackedAs: " + trackingName + " Active: " + active);

            var branch = new GitBranch(name, trackingName, active);

            OnBranch(branch);
        }
    }
}