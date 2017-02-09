namespace GitHub.Api
{
    public interface IPlatform
    {
        ICredentialManager CredentialManager { get; }
        IGitEnvironment GitEnvironment { get; }
    }
}