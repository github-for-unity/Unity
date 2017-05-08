using UnityEditor;

namespace GitHub.Unity
{
    class RepositoryInitializer : RepositoryInitializerBase
    {
        public RepositoryInitializer(IApplicationManager applicationManager)
            : base(applicationManager)
        {
        }

        protected override void SetProjectToTextSerialization()
        {
            Logger.Trace("SetProjectToTextSerialization");

            new ActionTask(this.ApplicationManager.CancellationToken, () => { EditorSettings.serializationMode = SerializationMode.ForceText; })
                .ScheduleUI(TaskManager);
        }
    }
}