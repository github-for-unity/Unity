namespace GitHub.Unity
{
    public class GitCountObjectsProcessor : BaseOutputProcessor<int>
    {
        public override void LineReceived(string line)
        {
            if (line == null)
            {
                return;
            }

            //2488 objects, 4237 kilobytes

            try
            {
                var proc = new LineParser(line);

                proc.ReadUntil(',');
                proc.SkipWhitespace();
                var kilobytes = int.Parse(proc.ReadUntilWhitespace());

                RaiseOnEntry(kilobytes);
            }
            catch {}
            return;
        }
    }
}
