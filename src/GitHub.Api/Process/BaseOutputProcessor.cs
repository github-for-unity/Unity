using System;
using System.Text;

namespace GitHub.Unity
{
    class BaseOutputProcessor : IOutputProcessor
    {
        public event Action<string> OnData;

        public BaseOutputProcessor()
        {
            Logger = Logging.GetLogger(GetType());
        }

        public virtual void LineReceived(string line)
        {
            Logger.Trace("LineReceived {0}", line == null ? "[null]" : line);

            if (line == null)
            {
                return;
            }

            OnData?.Invoke(line);
        }

        protected ILogging Logger { get; private set; }
    }
}