using GitHub.Logging;
using System;
using System.Text.RegularExpressions;

namespace GitHub.Unity
{
    public struct TheVersion : IComparable<TheVersion>
    {
        private const string versionRegex = @"(?<major>\d+)(\.?(?<minor>[^.]+))?(\.?(?<patch>[^.]+))?(\.?(?<build>.+))?";
        private const int PART_COUNT = 4;
        public static TheVersion Default { get; } = default(TheVersion).Initialize(null);

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
        [NotSerialized] private string version;
        public string Version { get { if (version == null) version = String.Empty; return version; } set { version = value; } }

        private static readonly Regex regex = new Regex(versionRegex);

        public static TheVersion Parse(string version)
        {
            return default(TheVersion).Initialize(version);
        }

        private TheVersion Initialize(string theVersion)
        {
            if (initialized)
                return this;

            this.Version = theVersion?.Trim() ?? String.Empty;

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

            if (String.IsNullOrEmpty(theVersion))
                return this;

            var match = regex.Match(theVersion);
            if (!match.Success)
            {
                LogHelper.Error(new ArgumentException("Invalid version: " + theVersion, "theVersion"));
                return this;
            }

            major = int.Parse(match.Groups["major"].Value);
            intParts[parts] = major;
            stringParts[parts] = major.ToString();
            parts = 1;

            var minorMatch = match.Groups["minor"];
            var patchMatch = match.Groups["patch"];
            var buildMatch = match.Groups["build"];

            if (minorMatch.Success)
            {
                if (!int.TryParse(minorMatch.Value, out minor))
                {
                    special = minorMatch.Value.TrimEnd();
                    stringParts[parts] = special ?? "0";
                }
                else
                {
                    intParts[parts] = minor;
                    stringParts[parts] = minor.ToString();
                    parts++;

                    if (patchMatch.Success)
                    {
                        if (!int.TryParse(patchMatch.Value, out patch))
                        {
                            special = patchMatch.Value.TrimEnd();
                            stringParts[parts] = special ?? "0";
                        }
                        else
                        {
                            intParts[parts] = patch;
                            stringParts[parts] = patch.ToString();
                            parts++;

                            if (buildMatch.Success)
                            {
                                if (!int.TryParse(buildMatch.Value, out build))
                                {
                                    special = buildMatch.Value.TrimEnd();
                                    stringParts[parts] = special ?? "0";
                                }
                                else
                                {
                                    intParts[parts] = build;
                                    stringParts[parts] = build.ToString();
                                    parts++;
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
            return this;
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
            if (!lhs.initialized)
                return false;
            if (!rhs.initialized)
                return true;

            for (var i = 0; i < lhs.parts && i < rhs.parts; i++)
            {
                if (lhs.intParts[i] != rhs.intParts[i])
                    return lhs.intParts[i] > rhs.intParts[i];
            }

            for (var i = 1; i < PART_COUNT; i++)
            {
                var ret = CompareVersionStrings(lhs.stringParts[i], rhs.stringParts[i]);
                if (ret != 0)
                    return ret > 0;
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

        private static int CompareVersionStrings(string lhs, string rhs)
        {
            int lhsNonDigitPos;
            var lhsNumber = GetNumberFromVersionString(lhs, out lhsNonDigitPos);

            int rhsNonDigitPos;
            var rhsNumber = GetNumberFromVersionString(rhs, out rhsNonDigitPos);

            if (lhsNumber != rhsNumber)
                return lhsNumber.CompareTo(rhsNumber);

            if (lhsNonDigitPos < 0 && rhsNonDigitPos < 0)
                return 0;

            // versions with alphanumeric characters are always lower than ones without
            // i.e. 1.1alpha is lower than 1.1
            if (lhsNonDigitPos < 0)
                return 1;
            if (rhsNonDigitPos < 0)
                return -1;
            return lhs.Substring(lhsNonDigitPos).CompareTo(rhs.Substring(rhsNonDigitPos));
        }

        private static int GetNumberFromVersionString(string lhs, out int nonDigitPos)
        {
            nonDigitPos = IndexOfFirstNonDigit(lhs);
            var number = -1;
            if (nonDigitPos > -1)
            {
                int.TryParse(lhs.Substring(0, nonDigitPos), out number);
            }
            else
            {
                int.TryParse(lhs, out number);
            }
            return number;
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