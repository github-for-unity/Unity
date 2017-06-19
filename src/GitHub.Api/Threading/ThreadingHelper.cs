using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class ThreadingHelper
    {
        public static TaskScheduler MainThreadScheduler { get; set; }

        public static int MainThread { get; set; }
        public static bool InMainThread { get { return MainThread == 0 || Thread.CurrentThread.ManagedThreadId == MainThread; } }

        public static void SetMainThread()
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
        }

        public static bool InUIThread => (!Guard.InUnitTestRunner && InMainThread) || !(Guard.InUnitTestRunner);
    }
}