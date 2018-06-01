namespace GitHub.Unity
{
    public class GitCountObjectsProcessor : BaseOutputProcessor<GitCountObjects>
    {
        public override void LineReceived(string line)
        {
            if (line == null)
            {
                return;
            }

            //2488 objects, 4237 kilobytes

            var proc = new LineParser(line);

            var objects = int.Parse(proc.ReadUntilWhitespace());
            proc.ReadUntil(',');
            proc.SkipWhitespace();
            var kilobytes = int.Parse(proc.ReadUntilWhitespace());

            RaiseOnEntry(new GitCountObjects(objects, kilobytes));
        }
    }
}