using System;

namespace GitHub.Unity
{
    public static class ActionExtensions
    {
        public static void SafeInvoke(this Action action)
        {
            if (action != null)
                action();
        }

        public static void SafeInvoke<T>(this Action<T> action, T obj)
        {
            if (action != null)
                action(obj);
        }

        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 obj, T2 obj2)
        {
            if (action != null)
                action(obj, obj2);
        }
    }
}