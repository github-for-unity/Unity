using System;

namespace GitHub.Unity
{
    class LfsVersionOutputProcessor : BaseOutputProcessor<Version>
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

                RaiseOnEntry(new Version(Int32.Parse(strings[0]), Int32.Parse(strings[1]), Int32.Parse(strings[2])));
            }
        }
    }
}