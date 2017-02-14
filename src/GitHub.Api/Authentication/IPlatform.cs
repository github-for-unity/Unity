using GitHub.Unity;

namespace GitHub.Api
{
    interface IPlatform
    {
        ICredentialManager GetCredentialManager(IProcessManager processManager);
        IGitEnvironment GitEnvironment { get; }
    }
}