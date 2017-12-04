namespace GitHub.Unity
{
    class GitAheadBehindStatusOutputProcessor : BaseOutputProcessor<GitAheadBehindStatus>
    {
        public override void LineReceived(string line)
        {
            if (line == null)
            {
                return;
            }

            var proc = new LineParser(line);

            var ahead = int.Parse(proc.ReadUntilWhitespace());
            var behind = int.Parse(proc.ReadToEnd());

            RaiseOnEntry(new GitAheadBehindStatus(ahead, behind));
        }
    }
}