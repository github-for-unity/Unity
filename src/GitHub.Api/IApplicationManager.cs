using System.Threading;
using GitHub.Unity;

namespace GitHub.Unity
{
    interface IApplicationManager
    {
        CancellationToken CancellationToken { get; }
        IEnvironment Environment { get; }
        IFileSystem FileSystem { get; }
        IPlatform Platform { get; }
        IProcessEnvironment GitEnvironment { get; }
        IProcessManager ProcessManager { get; }
        ICredentialManager CredentialManager { get; }
        IGitClient GitClient { get; }
        ITaskResultDispatcher TaskResultDispatcher { get; }
        ISettings SystemSettings { get; }
        ISettings LocalSettings { get; }
        ISettings UserSettings { get; }
        GitObjectFactory GitObjectFactory { get; }
    }
}