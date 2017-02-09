using GitHub.Api;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GitHub.Unity
{
    class FileLogAdapter : ILogging
    {
        private static readonly object lk = new object();
        private readonly string contextPrefix;
        private readonly string filePath;

        private string Prefix
        {
            get
            {
                var time = DateTime.Now.ToString("HH:mm:ss tt");
                var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return string.Format("{0} [{1}] <{2}> ", time, threadId, contextPrefix);
            }
        }

        public FileLogAdapter(string path, string context)
        {
            contextPrefix = string.IsNullOrEmpty(context) 
                ? "GitHub" 
                : string.Format("GitHub:{0}", context);
            filePath = path;
        }

        private void Write(string message)
        {
            lock (lk)
            {
                try
                {
                    File.AppendAllText(filePath, message);
                }
                catch { }
            }
        }

        private void WriteLine(string message)
        {
            message = message + Environment.NewLine;
            Write(message);
        }

        private void Write(string format, params object[] objects)
        {
            WriteLine(String.Format(Prefix + format, objects));
        }

        public void Info(string message)
        {
            Write("INFO ");
            WriteLine(message);
        }

        public void Info(string format, params object[] objects)
        {
            Write("INFO ");
            Write(format, objects);
        }

        public void Debug(string message)
        {
#if DEBUG
            Write("DEBUG ");
            WriteLine(message);
#endif
        }

        public void Debug(string format, params object[] objects)
        {
#if DEBUG
            Write("DEBUG ");
            Write(format, objects);
#endif
        }

        public void Debug(Exception ex)
        {
#if DEBUG
            Write("DEBUG ");
            WriteLine(ex.Message);
            WriteLine(ex.StackTrace);
            var caller = Environment.StackTrace;
            var stack = caller.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            if (stack.Length > 2)
                caller = String.Join(Environment.NewLine, stack.Skip(2).ToArray());
            WriteLine(caller);
#endif
        }

        public void Warning(string message)
        {
            Write("WARN ");
            WriteLine(message);
        }

        public void Warning(string format, params object[] objects)
        {
            Write("WARN ");
            Write(format, objects);
        }

        public void Error(string message)
        {
            Write("ERROR ");
            WriteLine(message);
        }

        public void Error(string format, params object[] objects)
        {
            Write("ERROR ");
            Write(format, objects);
        }
    }
}