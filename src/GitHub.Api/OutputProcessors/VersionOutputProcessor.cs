using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class VersionOutputProcessor : BaseOutputProcessor<Version>
    {
        public static Regex GitVersionRegex = new Regex(@"git version ([\d]+)\.([\d]+)\.([\d]+)");

        public override void LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return;

            var match = GitVersionRegex.Match(line);

            if (match.Groups.Count > 0)
            {
                var major = Int32.Parse(match.Groups[1].Value);
                var minor = Int32.Parse(match.Groups[2].Value);
                var build = Int32.Parse(match.Groups[3].Value);
                var version = new Version(major, minor, build);
                RaiseOnEntry(version);
            }
        }
    }
}