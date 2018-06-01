using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class LfsVersionOutputProcessor : BaseOutputProcessor<TheVersion>
    {
        public override bool LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return false;

            var parts = line.Split('/', ' ');
            if (parts.Length > 1)
            {
                var version = TheVersion.Parse(parts[1]);
                RaiseOnEntry(version);
            }
            return false;
        }
    }
}