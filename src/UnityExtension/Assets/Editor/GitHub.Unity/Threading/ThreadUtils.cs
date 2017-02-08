using System.Threading;

namespace GitHub.Unity
{
    static class ThreadUtils
    {
        public static int MainThread { get; set; }
        public static bool InMainThread { get { return MainThread == 0 || Thread.CurrentThread.ManagedThreadId == MainThread; } }
        public static void SetMainThread()
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
        }
    }
}