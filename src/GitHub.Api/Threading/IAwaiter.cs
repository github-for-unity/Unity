using System.Runtime.CompilerServices;

namespace GitHub.Unity
{
    interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }
}