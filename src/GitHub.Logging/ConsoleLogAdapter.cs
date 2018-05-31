using System;
using System.Threading;

namespace GitHub.Logging
{
    public class ConsoleLogAdapter : LogAdapterBase
    {
        private string GetMessage(string context, string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff tt");
            return $"{time} {context} {message}";
        }

        public override void Info(string context, string message)
        {
            WriteLine(context, message);
        }

        public override void Debug(string context, string message)
        {
            WriteLine(context, message);
        }

        public override void Trace(string context, string message)
        {
            WriteLine(context, message);
        }

        public override void Warning(string context, string message)
        {
            WriteLine(context, message);
        }

        public override void Error(string context, string message)
        {
            WriteLine(context, message);
        }

        private void WriteLine(string context, string message)
        {
            Console.WriteLine(GetMessage(context, message));
        }
    }
}
