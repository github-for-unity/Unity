using UnityEditor;

namespace GitHub.Unity
{
    class RepositoryInitializer : RepositoryInitializerBase
    {
        public RepositoryInitializer(IEnvironment environment, IProcessManager processManager, ITaskQueueScheduler scheduler, IApplicationManager applicationManager) : base(environment, processManager, scheduler, applicationManager)
        { }

        protected override void SetProjectToTextSerialization()
        {
            Logger.Trace("SetProjectToTextSerialization");

            TaskRunner.Add(new SimpleTask(() => { EditorSettings.serializationMode = SerializationMode.ForceText; },
                ThreadingHelper.MainThreadScheduler));
        }
    }
}