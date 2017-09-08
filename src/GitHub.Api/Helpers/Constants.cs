using System;

namespace GitHub.Unity
{
    static class Constants
    {
        public const string GuidKey = "Guid";
        public const string MetricsKey = "MetricsEnabled";
        public const string UsageFile = "usage.json";
        public const string GitInstallPathKey = "GitInstallPath";
        public const string TraceLoggingKey = "EnableTraceLogging";

        public static readonly Version MinimumGitVersion = new Version(2, 11, 0);
        public static readonly Version MinimumGitLfsVersion = new Version(2, 2, 0);
    }
}