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

        public SoftwareVersion(string major, string minor, string build):
            this(int.Parse(major), int.Parse(minor), int.Parse(build))
        {
            
        }
    }
}