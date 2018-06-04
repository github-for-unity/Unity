using System.Reflection;

namespace GitHub.Unity
{
    public static class ApplicationConfiguration
    {
        public const int DefaultWebTimeout = 3000;
        public const int DefaultGitTimeout = 5000;
        public static int WebTimeout { get; set; } = DefaultWebTimeout;
        public static int GitTimeout { get; set; } = DefaultGitTimeout;
    }
}
