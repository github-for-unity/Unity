namespace GitHub.Api
{
    class WindowsBackendFactory : IBackendFactory
    {
        private static readonly ICredentialManager credentialManager = new WindowsCredentialManager();
        public ICredentialManager CredentialManager { get { return credentialManager; } }
    }
}