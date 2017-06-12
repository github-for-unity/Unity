using System;
using GitHub.Unity;
using System.Threading;

namespace TestUtils
{
    class TestSynchronizationContext : SynchronizationContext, IMainThreadSynchronizationContext
    {
        public void Schedule(Action action)
        {
            action();
        }
    }
}