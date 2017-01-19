namespace GitHub.Unity.Logging
{
    public interface ILogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogFormat(string format, params object[] objects);
        void LogWarningFormat(string format, params object[] objects);
        void LogErrorFormat(string format, params object[] objects);
    }
}