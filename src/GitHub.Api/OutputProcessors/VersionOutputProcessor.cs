using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class VersionOutputProcessor : BaseOutputProcessor<TheVersion>
    {
        public static Regex GitVersionRegex = new Regex(@"git version (.*)");

        public override void LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return;

            var match = GitVersionRegex.Match(line);
            if (match.Groups.Count > 1)
            {
                var version = TheVersion.Parse(match.Groups[1].Value);
                RaiseOnEntry(version);
            }
        }
    }
}