using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    class LfsVersionOutputProcessor : BaseOutputProcessor<Version>
    {
        public static Regex GitLfsVersionRegex = new Regex(@"git-lfs/([\d]+)\.([\d]+)\.([\d]+)");

        public override void LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return;

            var match = GitLfsVersionRegex.Match(line);

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