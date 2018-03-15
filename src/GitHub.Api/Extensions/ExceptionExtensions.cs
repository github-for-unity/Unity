using System;
using System.Threading;

namespace GitHub.Unity
{
    static class ExceptionExtensions
    {
        /// <summary>
        /// Represents exceptions we should never attempt to catch and ignore.
        /// </summary>
        /// <param name="exception">The exception being thrown.</param>
        /// <returns></returns>
        public static bool IsCriticalException(this Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            return exception.IsFatalException()
                || exception is AppDomainUnloadedException
                || exception is BadImageFormatException
                || exception is CannotUnloadAppDomainException
                || exception is InvalidProgramException
                || exception is NullReferenceException
                || exception is ArgumentException;
        }

        /// <summary>
        /// Represents exceptions we should never attempt to catch and ignore when executing third party plugin code.
        /// This is not as extensive as a proposed IsCriticalException method that I want to write for our own code.
        /// </summary>
        /// <param name="exception">The exception being thrown.</param>
        /// <returns></returns>
        public static bool IsFatalException(this Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            return exception is StackOverflowException
                || exception is OutOfMemoryException
                || exception is ThreadAbortException
                || exception is AccessViolationException;
        }

        public static bool CanRetry(this Exception exception)
        {
            return !exception.IsCriticalException()
                && !(exception is ObjectDisposedException);
        }
    }
}
