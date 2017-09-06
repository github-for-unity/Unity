namespace GitHub.Unity
{
    static class Constants
    {
        public const string GuidKey = "Guid";
        public const string MetricsKey = "MetricsEnabled";
        public const string UsageFile = "usage.json";
        public const string GitInstallPathKey = "GitInstallPath";
        public const string TraceLoggingKey = "EnableTraceLogging";

        public static readonly SoftwareVersion MinimumGitVersion = new SoftwareVersion(2, 11, 1);
        public static readonly SoftwareVersion MinimumGitLfsVersion = new SoftwareVersion(2, 0, 2);
    }
}