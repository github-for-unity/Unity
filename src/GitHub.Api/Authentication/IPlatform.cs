using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IPlatform
    {
        Task<IPlatform> Initialize(IProcessManager processManager, ITaskManager taskManager);
        IProcessEnvironment GitEnvironment { get; }
        ICredentialManager CredentialManager { get; }
        IEnvironment Environment { get; }
        IProcessManager ProcessManager { get; }
        IKeychain Keychain { get; }
    }
}