namespace GitHub.Api
{
    public interface IBackendFactory
    {
        ICredentialManager CredentialManager { get; }
    }
}