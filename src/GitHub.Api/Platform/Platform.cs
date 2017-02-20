using GitHub.Unity;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class Platform : IPlatform
    {
        private readonly IEnvironment environment;

        public Platform(IEnvironment environment, IFileSystem fs)
        {
            this.environment = environment;
            this.FileSystemWatchFactory = new PlatformFileSystemWatchFactory(environment);

            if (environment.IsWindows)
            {
                GitEnvironment = new WindowsGitEnvironment(environment, fs);
            }
            else if (environment.IsMac)
            {
                GitEnvironment = new MacGitEnvironment(environment, fs);
            }
            else
            {
                GitEnvironment = new LinuxGitEnvironment(environment, fs);
            }
        }

        public Task<IPlatform> Initialize(IProcessManager processManager)
        {
            if (CredentialManager == null)
            {
                if (environment.IsWindows)
                {
                    CredentialManager = new WindowsCredentialManager(environment, processManager);
                }
                else if (environment.IsMac)
                {
                    CredentialManager = new MacCredentialManager();
                }
                else
                {
                    CredentialManager = new LinuxCredentialManager();
                }
            }
            return TaskEx.FromResult(this as IPlatform);
        }

        public IProcessEnvironment GitEnvironment { get; private set; }
        public ICredentialManager CredentialManager { get; private set; }
        public IFileSystemWatchFactory FileSystemWatchFactory { get; private set; }
    }
}