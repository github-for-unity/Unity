namespace GitHub.Api
{
    class LinuxBackendFactory : IBackendFactory
    {
        private static readonly ICredentialManager credentialManager = new LinuxCredentialManager();
        public ICredentialManager CredentialManager { get { return credentialManager; } }
    }
}