using System;

namespace GitHub.Unity
{
    static class Constants
    {
        public const string GuidKey = "Guid";
        public const string MetricsKey = "MetricsEnabled";
        public const string UsageFile = "metrics.json";
        public const string GitInstallPathKey = "GitInstallPath";
        public const string TraceLoggingKey = "EnableTraceLogging";
        public const string WebTimeoutKey = "WebTimeout";
        public const  string Iso8601Format = @"yyyy-MM-dd\THH\:mm\:ss.fffzzz";
        public static readonly string[] Iso8601Formats = {
            @"yyyy-MM-dd\THH\:mm\:ss.fffffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.fffzzz",
            @"yyyy-MM-dd\THH\:mm\:sszzz"
        };

        public static readonly Version MinimumGitVersion = new Version(2, 11, 0);
        public static readonly Version MinimumGitLfsVersion = new Version(2, 3, 4);
    }
}