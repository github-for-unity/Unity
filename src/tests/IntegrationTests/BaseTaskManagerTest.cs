using System.Threading;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseTaskManagerTest : BaseIntegrationTest
    {
        protected ITaskManager TaskManager { get; private set; }
        protected SynchronizationContext SyncContext { get; set; }

        protected void InitializeTaskManager()
        {
            TaskManager = new TaskManager();
            SyncContext = new ThreadSynchronizationContext(TaskManager.Token);
            TaskManager.UIScheduler = new SynchronizationContextTaskScheduler(SyncContext);
        }
    }
}