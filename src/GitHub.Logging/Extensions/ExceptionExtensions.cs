using System;
using System.Linq;

namespace GitHub.Unity
{
    static class ExceptionExtensions
    {
        public static string GetExceptionMessage(this Exception ex)
        {
            var message = ex.Message + Environment.NewLine + ex.StackTrace;
            var caller = Environment.StackTrace;
            var stack = caller.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            if (stack.Length > 2)
            {
                message = message + Environment.NewLine + String.Join(Environment.NewLine, stack.Skip(2).ToArray());
            }
            return message;
        }
    }
}