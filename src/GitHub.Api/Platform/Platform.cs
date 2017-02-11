namespace GitHub.Api
{
    class Platform : IPlatform
    {
        public Platform(IEnvironment environment, IFileSystem fs)
        {
            if (environment.IsWindows)
            {
                CredentialManager =  new WindowsCredentialManager();
                GitEnvironment = new WindowsGitEnvironment(environment, fs);
            }
            else if (environment.IsMac)
            {
                CredentialManager = new MacCredentialManager();
                GitEnvironment = new MacGitEnvironment(environment, fs);
            }
            else
            {
                CredentialManager = new LinuxCredentialManager();
                GitEnvironment = new LinuxGitEnvironment(environment, fs);
            }
        }

        public ICredentialManager CredentialManager { get; private set; }
        public IGitEnvironment GitEnvironment { get; private set; }
    }
}