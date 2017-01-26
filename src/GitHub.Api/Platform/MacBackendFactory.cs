namespace GitHub.Api
{
    class MacBackendFactory : IBackendFactory
    {
        private static readonly ICredentialManager credentialManager = new MacCredentialManager();
        public ICredentialManager CredentialManager { get { return credentialManager; } }
    }
}