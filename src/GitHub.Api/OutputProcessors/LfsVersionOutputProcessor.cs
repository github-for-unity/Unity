using System;

namespace GitHub.Unity
{
    class LfsVersionOutputProcessor : BaseOutputProcessor<SoftwareVersion>
    {
        public override void LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return;

            var gitVersion = "git-lfs/";
            if (line.StartsWith(gitVersion))
            {
                line = line.Substring(gitVersion.Length, line.IndexOf(" ", StringComparison.InvariantCultureIgnoreCase) - gitVersion.Length);
                var strings = line.Split(new[] { "." }, StringSplitOptions.None);

                RaiseOnEntry(new SoftwareVersion(strings[0], strings[1], strings[2]));
            }
        }
    }
}