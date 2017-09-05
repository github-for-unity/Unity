using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class VersionOutputProcessor : BaseOutputProcessor<SoftwareVersion>
    {
        public override void LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return;

            var gitVersion = "git version ";
            if (line.StartsWith(gitVersion))
            {
                line = line.Substring(gitVersion.Length);
                var strings = line.Split(new[] { "." }, StringSplitOptions.None);

                RaiseOnEntry(new SoftwareVersion(strings[0], strings[1], strings[2]));
            }
        }
    }
}