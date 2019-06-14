using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    public class WindowsDiskUsageOutputProcessor : BaseOutputProcessor<long>
    {
        private int index = -1;
        private int lineCount = 0;
        private string[] buffer = new string[2];
        //           199854 File(s) 25,835,841,045 bytes
        private static readonly Regex totalFileCount = new Regex(@"[\s]*[\d]+[\s]+File\(s\)[\s]+(?<bytes>[^\s]+)",
            RegexOptions.Compiled);
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

                var match = totalFileCount.Match(output);
                long kilobytes = 0;
                if (match.Success)
                {
                    var bytes = long.Parse(match.Groups["bytes"].Value.Replace(",", String.Empty).Replace(".", String.Empty));
                    kilobytes = bytes / 1024;
                }

                RaiseOnEntry(kilobytes);
            }
            else
            {
                buffer[index] = line;
            }
        }
    }
}
