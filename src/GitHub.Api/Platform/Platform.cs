using GitHub.Unity;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class Platform : IPlatform
    {
        public Platform(IEnvironment environment, IFileSystem filesystem, IUIDispatcher uiDispatcher)
        {
            Environment = environment;
            UIDispatcher = uiDispatcher;
            FileSystemWatchFactory = new PlatformFileSystemWatchFactory(environment);

            if (environment.IsWindows)
            {
                GitEnvironment = new WindowsGitEnvironment(environment, filesystem);
            }
            else if (environment.IsMac)
            {
                GitEnvironment = new MacGitEnvironment(environment, filesystem);
            }
            else
            {
                GitEnvironment = new LinuxGitEnvironment(environment, filesystem);
            }
        }

        public Task<IPlatform> Initialize(IProcessManager processManager)
        {
            ProcessManager = processManager;

            if (CredentialManager == null)
            {
                CredentialManager = new GitCredentialManager(Environment, processManager);
            }
            return TaskEx.FromResult(this as IPlatform);
        }

        public IEnvironment Environment { get; private set; }
        public IProcessEnvironment GitEnvironment { get; private set; }
        public ICredentialManager CredentialManager { get; private set; }
        public IFileSystemWatchFactory FileSystemWatchFactory { get; private set; }
        public IProcessManager ProcessManager { get; private set; }
        public IUIDispatcher UIDispatcher { get; private set; }

    }
}