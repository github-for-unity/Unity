namespace GitHub.Api
{
    interface IPlatform
    {
        ICredentialManager CredentialManager { get; }
        IGitEnvironment GitEnvironment { get; }
    }
}