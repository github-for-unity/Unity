using System;

namespace GitHub.Logging
{
    public interface ILogging
    {
        void Info(string message);
        void Info(string format, params object[] objects);
        void Info(Exception ex, string message);
        void Info(Exception ex);
        void Info(Exception ex, string format, params object[] objects);

        void Debug(string message);
        void Debug(string format, params object[] objects);
        void Debug(Exception ex);
        void Debug(Exception ex, string message);
        void Debug(Exception ex, string format, params object[] objects);

        void Trace(string message);
        void Trace(string format, params object[] objects);
        void Trace(Exception ex);
        void Trace(Exception ex, string message);
        void Trace(Exception ex, string format, params object[] objects);

        void Warning(string message);
        void Warning(string format, params object[] objects);
        void Warning(Exception ex);
        void Warning(Exception ex, string message);
        void Warning(Exception ex, string format, params object[] objects);

        void Error(string message);
        void Error(string format, params object[] objects);
        void Error(Exception ex);
        void Error(Exception ex, string message);
        void Error(Exception ex, string format, params object[] objects);
    }
}
