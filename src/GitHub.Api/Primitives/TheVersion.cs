using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    public struct TheVersion : IComparable<TheVersion>
    {
        private const string versionRegex = @"(?<major>\d+)(\.?(?<minor>[^.]+))?(\.?(?<patch>[^.]+))?(\.?(?<build>.+))?";
        private const int PART_COUNT = 4;

        [NotSerialized] private int major;
        [NotSerialized] public int Major { get { Initialize(Version); return major; } }
        [NotSerialized] private int minor;
        [NotSerialized] public int Minor { get { Initialize(Version); return minor; } }
        [NotSerialized] private int patch;
        [NotSerialized] public int Patch { get { Initialize(Version); return patch; } }
        [NotSerialized] private int build;
        [NotSerialized] public int Build { get { Initialize(Version); return build; } }
        [NotSerialized] private string special;
        [NotSerialized] public string Special { get { Initialize(Version); return special; } }
        [NotSerialized] private bool isAlpha;
        [NotSerialized] public bool IsAlpha { get { Initialize(Version); return isAlpha; } }
        [NotSerialized] private bool isBeta;
        [NotSerialized] public bool IsBeta { get { Initialize(Version); return isBeta; } }
        [NotSerialized] private bool isUnstable;
        [NotSerialized] public bool IsUnstable { get { Initialize(Version); return isUnstable; } }

        [NotSerialized] private int[] intParts;
        [NotSerialized] private string[] stringParts;
        [NotSerialized] private int parts;
        [NotSerialized] private bool initialized;

        public string Version { get; set; }

        private static readonly Regex regex = new Regex(versionRegex);

        public static TheVersion Parse(string version)
        {
            Guard.ArgumentNotNull(version, "version");
            TheVersion ret = default(TheVersion);
            ret.Initialize(version);
            return ret;
        }

        private void Initialize(string version)
        {
            if (initialized)
                return;

            this.Version = version;

            isAlpha = false;
            isBeta = false;
            major = 0;
            minor = 0;
            patch = 0;
            build = 0;
            special = null;
            parts = 0;

            intParts = new int[PART_COUNT];
            stringParts = new string[PART_COUNT];

            for (var i = 0; i < PART_COUNT; i++)
                stringParts[i] = intParts[i].ToString();

            var match = regex.Match(version);
            if (!match.Success)
                throw new ArgumentException("Invalid version: " + version, "version");

            major = int.Parse(match.Groups["major"].Value);
            intParts[0] = major;
            parts = 1;

            var minorMatch = match.Groups["minor"];
            var patchMatch = match.Groups["patch"];
            var buildMatch = match.Groups["build"];

            if (minorMatch.Success)
            {
                parts++;
                if (!int.TryParse(minorMatch.Value, out minor))
                {
                    special = minorMatch.Value;
                    stringParts[parts - 1] = special;
                }
                else
                {
                    intParts[parts - 1] = minor;

                    if (patchMatch.Success)
                    {
                        parts++;
                        if (!int.TryParse(patchMatch.Value, out patch))
                        {
                            special = patchMatch.Value;
                            stringParts[parts - 1] = special;
                        }
                        else
                        {
                            intParts[parts - 1] = patch;

                            if (buildMatch.Success)
                            {
                                parts++;
                                if (!int.TryParse(buildMatch.Value, out build))
                                {
                                    special = buildMatch.Value;
                                    stringParts[parts - 1] = special;
                                }
                                else
                                {
                                    intParts[parts - 1] = build;
                                }
                            }
                        }
                    }
                }
            }

            isUnstable = special != null;
            if (isUnstable)
            {
                isAlpha = special.IndexOf("alpha") >= 0;
                isBeta = special.IndexOf("beta") >= 0;
            }
            initialized = true;
        }

        public override string ToString()
        {
            return Version;
        }

        public int CompareTo(TheVersion other)
        {
            if (this > other)
                return 1;
            if (this == other)
                return 0;
            return -1;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Major.GetHashCode();
            hash = hash * 23 + Minor.GetHashCode();
            hash = hash * 23 + Patch.GetHashCode();
            hash = hash * 23 + Build.GetHashCode();
            hash = hash * 23 + (Special != null ? Special.GetHashCode() : 0);
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj is TheVersion)
                return Equals((TheVersion)obj);
            return false;
        }

        public bool Equals(TheVersion other)
        {
            return this == other;
        }

        public static bool operator==(TheVersion lhs, TheVersion rhs)
        {
            if (lhs.Version == rhs.Version)
                return true;
            return
                (lhs.Major == rhs.Major) &&
                    (lhs.Minor == rhs.Minor) &&
                    (lhs.Patch == rhs.Patch) &&
                    (lhs.Build == rhs.Build) &&
                    (lhs.Special == rhs.Special);
        }

        public static bool operator!=(TheVersion lhs, TheVersion rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator>(TheVersion lhs, TheVersion rhs)
        {
            if (lhs.Version == rhs.Version)
                return false;
            if (lhs.Version == null)
                return false;
            if (rhs.Version == null)
                return true;

            for (var i = 0; i < PART_COUNT; i++)
            {
                if (lhs.intParts[i] != rhs.intParts[i])
                    return lhs.intParts[i] > rhs.intParts[i];
            }

            for (var i = 1; i < PART_COUNT; i++)
            {
                if (lhs.stringParts[i] != rhs.stringParts[i])
                {
                    return GreaterThan(lhs.stringParts[i], rhs.stringParts[i]);
                }
            }
            return false;
        }

        public static bool operator<(TheVersion lhs, TheVersion rhs)
        {
            return !(lhs > rhs);
        }

        public static bool operator>=(TheVersion lhs, TheVersion rhs)
        {
            return lhs > rhs || lhs == rhs;
        }

        public static bool operator<=(TheVersion lhs, TheVersion rhs)
        {
            return lhs < rhs || lhs == rhs;
        }

        private static bool GreaterThan(string lhs, string rhs)
        {
            var lhsNonDigitPos = IndexOfFirstNonDigit(lhs);
            var rhsNonDigitPos = IndexOfFirstNonDigit(rhs);

            var lhsNumber = -1;
            if (lhsNonDigitPos > -1)
            {
                lhsNumber = int.Parse(lhs.Substring(0, lhsNonDigitPos));
            }
            else
            {
                int.TryParse(lhs, out lhsNumber);
            }

            var rhsNumber = -1;
            if (rhsNonDigitPos > -1)
            {
                rhsNumber = int.Parse(rhs.Substring(0, rhsNonDigitPos));
            }
            else
            {
                int.TryParse(rhs, out rhsNumber);
            }

            if (lhsNumber != rhsNumber)
                return lhsNumber > rhsNumber;

            return lhs.Substring(lhsNonDigitPos > -1 ? lhsNonDigitPos : 0).CompareTo(rhs.Substring(rhsNonDigitPos > -1 ? rhsNonDigitPos : 0)) > 0;
        }

        private static int IndexOfFirstNonDigit(string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                if (!char.IsDigit(str[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}