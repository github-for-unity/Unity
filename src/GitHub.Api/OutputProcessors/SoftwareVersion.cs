namespace GitHub.Unity
{
    public struct SoftwareVersion
    {
        public int Major;
        public int Minor;
        public int Build;

        public SoftwareVersion(int major, int minor, int build)
        {
            Major = major;
            Minor = minor;
            Build = build;
        }

        public SoftwareVersion(string major, string minor, string build) :
            this(int.Parse(major), int.Parse(minor), int.Parse(build))
        {

        }

        public static bool operator <(SoftwareVersion lhs, SoftwareVersion rhs)
        {
            if (lhs.Major < rhs.Major)
            {
                return true;
            }

            if (lhs.Major == rhs.Major)
            {
                if (lhs.Minor < rhs.Minor)
                {
                    return true;
                }

                if (lhs.Minor == rhs.Minor)
                {
                    if (lhs.Build < rhs.Build)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool operator >(SoftwareVersion lhs, SoftwareVersion rhs)
        {
            if (lhs.Major > rhs.Major)
            {
                return true;
            }

            if (lhs.Major == rhs.Major)
            {
                if (lhs.Minor > rhs.Minor)
                {
                    return true;
                }

                if (lhs.Minor == rhs.Minor)
                {
                    if (lhs.Build > rhs.Build)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool operator <=(SoftwareVersion lhs, SoftwareVersion rhs)
        {
            return lhs.Equals(rhs) || lhs < rhs;
        }

        public static bool operator >=(SoftwareVersion lhs, SoftwareVersion rhs)
        {
            return lhs.Equals(rhs) || lhs > rhs;
        }
    }
}