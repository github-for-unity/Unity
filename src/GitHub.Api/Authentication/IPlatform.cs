namespace GitHub.Api
{
    public interface IPlatform
    {
        ICredentialManager CredentialManager { get; }
    }
}