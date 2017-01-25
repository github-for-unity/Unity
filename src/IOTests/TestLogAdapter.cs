using System;
using GitHub.Unity.Logging;

namespace IOTests
{
    class TestLogAdapter : ILogger
    {
        private readonly string _prefix;

        public TestLogAdapter(string context = null)
        {
            _prefix = string.IsNullOrEmpty(context) 
                ? string.Empty 
                : context + ": ";
        }

        public void Info(string message)
        {
            Console.WriteLine(_prefix + message);
        }

        public void Info(string format, params object[] objects)
        {
            Console.WriteLine(_prefix + format, objects);
        }

        public void Debug(string message)
        {
            Console.WriteLine(_prefix + message);
        }

        public void Debug(string format, params object[] objects)
        {
            Console.WriteLine(_prefix + format, objects);
        }

        public void Warning(string message)
        {
            Console.WriteLine(_prefix + message);
        }

        public void Warning(string format, params object[] objects)
        {
            Console.WriteLine(_prefix + format, objects);
        }

        public void Error(string message)
        {
            Console.WriteLine(_prefix + message);
        }

        public void Error(string format, params object[] objects)
        {
            Console.WriteLine(_prefix + format, objects);
        }
    }
}