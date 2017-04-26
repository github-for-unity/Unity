using System;
using System.Linq;

namespace GitHub.Unity
{
    class MultipleLogAdapter : LogAdapterBase
    {
        private ILogging[] loggers;

        public MultipleLogAdapter(params Func<ILogging>[] logFunctions) : base(string.Empty)
        {
            loggers = logFunctions.Select(func => func.Invoke()).ToArray();
        }

        protected override void OnInfo(string message)
        {
            foreach (var logger in loggers)
            {
                logger.Info(message);
            }
        }

        protected override void OnDebug(string message)
        {
            foreach (var logger in loggers)
            {
                logger.Info(message);
            }
        }

        protected override void OnTrace(string message)
        {
            foreach (var logger in loggers)
            {
                logger.Info(message);
            }
        }

        protected override void OnWarning(string message)
        {
            foreach (var logger in loggers)
            {
                logger.Info(message);
            }
        }

        protected override void OnError(string message)
        {
            foreach (var logger in loggers)
            {
                logger.Info(message);
            }
        }
    }
}