using System;

namespace GitHub.Unity
{
    public interface ILogging
    {
        void Info(string message);
        void Info(string format, params object[] objects);

        void Debug(string message);
        void Debug(string format, params object[] objects);
        void Debug(Exception ex);

        void Warning(string message);
        void Warning(string format, params object[] objects);

        void Error(string message);
        void Error(string format, params object[] objects);
    }
}