using GitHub.Unity;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IPlatform
    {
        Task<IPlatform> Initialize(IProcessManager processManager, ISettings settings);
        IProcessEnvironment GitEnvironment { get; }
        ICredentialManager CredentialManager { get; }
        IEnvironment Environment { get; }
        IProcessManager ProcessManager { get; }
        IUIDispatcher UIDispatcher { get; }
        IKeychain Keychain { get; }
    }
}