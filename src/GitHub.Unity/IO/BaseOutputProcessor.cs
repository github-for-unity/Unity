using System;
using System.Text;

namespace GitHub.Unity
{
    class BaseOutputProcessor : IOutputProcessor
    {
        public event Action<string> OnData;

        public virtual void LineReceived(string line)
        {
            if (line == null)
            {
                return;
            }
            OnData.Invoke(line);
        }
    }
}