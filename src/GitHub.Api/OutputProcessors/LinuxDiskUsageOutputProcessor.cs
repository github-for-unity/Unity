using System;

namespace GitHub.Unity
{
    public class LinuxDiskUsageOutputProcessor : BaseOutputProcessor<int>
    {
        private string buffer;

        public override void LineReceived(string line)
        {
            if (line == null)
            {
                if (buffer == null)
                {
                    throw new InvalidOperationException("Not enough input");
                }

                var proc = new LineParser(buffer);
                var kilobytes = int.Parse(proc.ReadUntilWhitespace());

                RaiseOnEntry(kilobytes);
            }
            else
            {
                buffer = line;
            }
        }
    }
}