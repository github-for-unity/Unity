using GitHub.Unity;

namespace TestUtils.Events
{
    interface IRepositoryListener
    {
    }

    class RepositoryEvents
    {
        public void Reset()
        {
        }
    }

    static class RepositoryListenerExtensions
    {
        public static void AttachListener(this IRepositoryListener listener,
            IRepository repository, RepositoryEvents repositoryEvents = null, bool trace = true)
        {
            //var logger = trace ? LogHelper.GetLogger<IRepositoryListener>() : null;
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryListener repositoryListener)
        {
        }
    }
};