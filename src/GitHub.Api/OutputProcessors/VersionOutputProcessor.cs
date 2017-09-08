using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class VersionOutputProcessor : BaseOutputProcessor<Version>
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

                RaiseOnEntry(new Version(Int32.Parse(strings[0]), Int32.Parse(strings[1]), Int32.Parse(strings[2])));
            }
        }
    }
}