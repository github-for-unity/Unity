using System;

namespace GitHub.Unity
{
    public class LinuxDiskUsageOutputProcessor : BaseOutputProcessor<int>
    {
        public override void LineReceived(string line)
        {
            if (line == null)
                return;

            int kb;
            var proc = new LineParser(line);
            if (int.TryParse(proc.ReadUntilWhitespace(), out kb))
                RaiseOnEntry(kb);
        }
    }
}
