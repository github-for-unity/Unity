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
        public const string Iso8601Format = @"yyyy-MM-dd\THH\:mm\:ss.fffzzz";
        public const string Iso8601FormatZ = @"yyyy-MM-dd\THH\:mm\:ss\Z";
        public static readonly string[] Iso8601Formats = {
            @"yyyy-MM-dd\THH\:mm\:ss\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.fffffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.fffzzz",
            @"yyyy-MM-dd\THH\:mm\:sszzz",
        };
        public const string SkipVersionKey = "SkipVersion";
        public const string GitInstallationState = "GitInstallationState";

        public static readonly TheVersion MinimumGitVersion = TheVersion.Parse("2.11");
        public static readonly TheVersion MinimumGitLfsVersion = TheVersion.Parse("2.0");
    }
}
