using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class ConfigOutputProcessor : BaseOutputProcessor
    {
        private static readonly ILogging logger = Logging.GetLogger<ConfigOutputProcessor>();

        public event Action<KeyValuePair<string, string>> OnEntry;

        public override void LineReceived(string line)
        {
            base.LineReceived(line);

            if (String.IsNullOrEmpty(line) || OnEntry == null)
                return;

            var eqs = line.IndexOf("=");
            if (eqs <= 0)
            {
                return;
            }
            var kvp = new KeyValuePair<string, string>(line.Substring(0, eqs), line.Substring(eqs + 1));
            OnEntry(kvp);
        }
    }
}