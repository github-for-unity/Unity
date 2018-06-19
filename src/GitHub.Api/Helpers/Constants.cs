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
        public const string Iso8601FormatPointZ = @"yyyy-MM-dd\THH\:mm\:ss.ff\Z";
        public static readonly string[] Iso8601Formats = {
            Iso8601FormatZ,
            @"yyyy-MM-dd\THH\:mm\:ss.fffffffzzz",
            Iso8601Format,
            Iso8601FormatPointZ,
            @"yyyy-MM-dd\THH\:mm\:sszzz",
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
