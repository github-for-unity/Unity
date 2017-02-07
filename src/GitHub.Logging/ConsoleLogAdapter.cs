using GitHub.Unity;
using System;

namespace GitHub.Logging
{
    class ConsoleLogAdapter : ILogging
    {
        private readonly string prefix;

        public ConsoleLogAdapter(string context = null)
        {
            prefix = string.IsNullOrEmpty(context) 
                ? string.Empty 
                : context + ": ";
        }

        public void Info(string message)
        {
            Console.WriteLine(prefix + message);
        }

        public void Info(string format, params object[] objects)
        {
            Console.WriteLine(prefix + format, objects);
        }

        public void Debug(string message)
        {
            Console.WriteLine(prefix + message);
        }

        public void Debug(string format, params object[] objects)
        {
            Console.WriteLine(prefix + format, objects);
        }

        public void Debug(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void Warning(string message)
        {
            Console.WriteLine(prefix + message);
        }

        public void Warning(string format, params object[] objects)
        {
            Console.WriteLine(prefix + format, objects);
        }

        public void Error(string message)
        {
            Console.WriteLine(prefix + message);
        }

        public void Error(string format, params object[] objects)
        {
            Console.WriteLine(prefix + format, objects);
        }
    }
}