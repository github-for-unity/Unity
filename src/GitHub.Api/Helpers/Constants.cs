using System.Globalization;

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
        public const string GitTimeoutKey = "GitTimeout";
        public const string Iso8601Format = @"yyyy-MM-dd\THH\:mm\:ss.fffzzz";
        public const string Iso8601FormatZ = @"yyyy-MM-dd\THH\:mm\:ss\Z";
        public static readonly string[] Iso8601Formats = {
            Iso8601Format,
            Iso8601FormatZ,
            @"yyyy-MM-dd\THH\:mm\:ss.fffffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.ffffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.fffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.ffffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.ffzzz",
            @"yyyy-MM-dd\THH\:mm\:ss.fzzz",
            @"yyyy-MM-dd\THH\:mm\:sszzz",
            @"yyyy-MM-dd\THH\:mm\:ss.fffffff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.ffffff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.fffff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.ffff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.fff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.ff\Z",
            @"yyyy-MM-dd\THH\:mm\:ss.f\Z",
        };
        public const DateTimeStyles DateTimeStyle = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        public const string SkipVersionKey = "SkipVersion";
        public const string GitInstallationState = "GitInstallationState";

        public static readonly TheVersion MinimumGitVersion = TheVersion.Parse("2.0");
        public static readonly TheVersion MinimumGitLfsVersion = TheVersion.Parse("2.0");
        public static readonly TheVersion DesiredGitVersion = TheVersion.Parse("2.11");
        public static readonly TheVersion DesiredGitLfsVersion = TheVersion.Parse("2.4");
    }
}
