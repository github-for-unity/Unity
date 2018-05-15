using System.Reflection;

namespace GitHub.Unity
{
    public static class ApplicationConfiguration
    {
        public const int DefaultWebTimeout = 3000;
        public static int WebTimeout { get; set; } = DefaultWebTimeout;
    }
}
