using GitHub.Unity;

namespace GitHub.Api
{
    class Platform : IPlatform
    {
        private readonly IEnvironment environment;

        private ICredentialManager credentialManager;

        public Platform(IEnvironment environment, IFileSystem fs)
        {
            this.environment = environment;
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

        public ICredentialManager GetCredentialManager(IProcessManager processManager)
        {
            if (credentialManager == null)
            {
                if (environment.IsWindows)
                {
                    credentialManager = new WindowsCredentialManager(environment, processManager);
                }
                else if (environment.IsMac)
                {
                    credentialManager = new MacCredentialManager();
                }
                else
                {
                    credentialManager = new LinuxCredentialManager();
                }
            }
            return credentialManager;
        }

        public IGitEnvironment GitEnvironment { get; private set; }
    }
}