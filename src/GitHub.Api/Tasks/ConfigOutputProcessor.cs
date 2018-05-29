using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class ConfigOutputProcessor : BaseOutputListProcessor<KeyValuePair<string, string>>
    {
        public override bool LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return false;

            var eqs = line.IndexOf("=");
            if (eqs <= 0)
            {
                return false;
            }
            var kvp = new KeyValuePair<string, string>(line.Substring(0, eqs), line.Substring(eqs + 1));
            RaiseOnEntry(kvp);
            return false;
        }
    }
}