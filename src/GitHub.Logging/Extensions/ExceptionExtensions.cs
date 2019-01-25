using System;
using System.Linq;
using GitHub.Unity;

namespace GitHub.Logging
{
    public static class ExceptionExtensions
    {
        public static string GetExceptionMessage(this Exception ex, bool includeEnvironment = false)
        {
            var message = ex.ToString();

            var processException = ex as ProcessException;
            if (includeEnvironment && processException != null)
            {
                message += Environment.NewLine + String.Join(Environment.NewLine, ((ProcessException)ex).EnvironmentVariables);
            }

            var inner = ex.InnerException;
            while (inner != null)
            {
                message += Environment.NewLine + inner.ToString();
                processException = inner as ProcessException;
                if (includeEnvironment && processException != null)
                {
                    message += Environment.NewLine + String.Join(Environment.NewLine, processException.EnvironmentVariables);
                }
                inner = inner.InnerException;
            }
            var caller = Environment.StackTrace;
            var stack = caller.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            message += Environment.NewLine + "=======";
            message += Environment.NewLine + String.Join(Environment.NewLine, stack.Skip(1).SkipWhile(x => x.Contains("GitHub.Logging")).ToArray());
            return message;
        }

        public static string GetExceptionMessageShort(this Exception ex)
        {
            var message = ex.ToString();
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += Environment.NewLine + inner.ToString();
                inner = inner.InnerException;
            }
            return message;
        }
    }
}
