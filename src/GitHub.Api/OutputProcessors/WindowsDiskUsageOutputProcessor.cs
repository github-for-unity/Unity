using System;

namespace GitHub.Unity
{
    public class WindowsDiskUsageOutputProcessor : BaseOutputProcessor<int>
    {
        private int index = -1;
        private int lineCount = 0;
        private string[] buffer = new string[2];

        public override void LineReceived(string line)
        {
            lineCount++;
            index = (index + 1) % 2;

            if (line == null)
            {
                if (lineCount <= 2)
                {
                    throw new InvalidOperationException("Not enough input");
                }

                var output = buffer[index];

                Logger.Trace("Processing: {0}", output);

                var proc = new LineParser(output);
                proc.SkipWhitespace();
                proc.ReadUntilWhitespace();
                proc.ReadUntilWhitespace();
                proc.SkipWhitespace();

                var bytes = int.Parse(proc.ReadUntilWhitespace().Replace(",", string.Empty));
                var kilobytes = bytes / 1024;
                RaiseOnEntry(kilobytes);
            }
            else
            {
                buffer[index] = line;
            }
        }
    }
}