using System;
using GitHub.Unity.Logging;

namespace IOTests
{
    public class TestLogAdapter : ILogger
    {
        private readonly string _prefix;

        public TestLogAdapter(string context = null)
        {
            _prefix = string.IsNullOrEmpty(context) 
                ? string.Empty 
                : context + ": ";
        }

        public void Log(string message)
        {
            Console.WriteLine(_prefix + message);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine(_prefix + message);
        }

        public void LogError(string message)
        {
            Console.WriteLine(_prefix + message);
        }

        public void LogFormat(string format, params object[] objects)
        {
            Console.WriteLine(_prefix + format, objects);
        }

        public void LogWarningFormat(string format, params object[] objects)
        {
            Console.WriteLine(_prefix + format, objects);
        }

        public void LogErrorFormat(string format, params object[] objects)
        {
            Console.WriteLine(_prefix + format, objects);
        }
    }
}