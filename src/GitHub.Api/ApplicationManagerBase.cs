using System.Threading;

namespace GitHub.Unity
{
    class ApplicationManagerBase : IApplicationManager
    {
        public CancellationToken CancellationToken { get; protected set; }
        public ICredentialManager CredentialManager { get; protected set; }
        public IFileSystem FileSystem { get; protected set; }
        public IGitClient GitClient { get; protected set; }
        public GitObjectFactory GitObjectFactory { get; protected set; }
        public ISettings LocalSettings { get; protected set; }
        public IPlatform Platform { get; protected set; }
        public IProcessManager ProcessManager { get; protected set; }
        public ISettings SystemSettings { get; protected set; }
        public ITaskResultDispatcher TaskResultDispatcher { get; protected set; }
        public ISettings UserSettings { get; protected set; }
        public virtual IGitEnvironment GitEnvironment { get; set; }
        public virtual IEnvironment Environment { get; set; }
    }
}