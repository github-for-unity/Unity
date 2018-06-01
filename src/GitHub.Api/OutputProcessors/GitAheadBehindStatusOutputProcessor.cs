namespace GitHub.Unity
{
    class GitAheadBehindStatusOutputProcessor : BaseOutputProcessor<GitAheadBehindStatus>
    {
        public override bool LineReceived(string line)
        {
            if (line == null)
                return false;

            var proc = new LineParser(line);
            var ahead = int.Parse(proc.ReadUntilWhitespace());
            var behind = int.Parse(proc.ReadToEnd());

            RaiseOnEntry(new GitAheadBehindStatus(ahead, behind));
            return false;
        }
    }
}