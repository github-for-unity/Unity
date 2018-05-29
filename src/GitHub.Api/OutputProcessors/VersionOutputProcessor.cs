using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class VersionOutputProcessor : BaseOutputProcessor<TheVersion>
    {
        public static Regex GitVersionRegex = new Regex(@"git version (.*)");

        public override bool LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return false;

            var match = GitVersionRegex.Match(line);
            if (match.Groups.Count > 1)
            {
                var version = TheVersion.Parse(match.Groups[1].Value);
                RaiseOnEntry(version);
            }
            return false;
        }
    }
}