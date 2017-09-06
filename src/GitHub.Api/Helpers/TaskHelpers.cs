using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class TaskHelpers
    {
        public static Task<T> GetCompletedTask<T>(T result)
        {
            return TaskEx.FromResult(result);
        }
    }
}