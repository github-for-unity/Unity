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

    class Platform : IPlatform
    {
        public Platform(IEnvironment environment)
        {
            Environment = environment;
            GitEnvironment = new ProcessEnvironment(environment);
        }

        public Task<IPlatform> Initialize(IProcessManager processManager, ITaskManager taskManager)
        {
            ProcessManager = processManager;

            if (CredentialManager == null)
            {
                CredentialManager = new GitCredentialManager(Environment, processManager, taskManager);
                Keychain = new Keychain(Environment, CredentialManager);
                Keychain.Initialize();
            }

            return TaskEx.FromResult(this as IPlatform);
        }

        public IEnvironment Environment { get; private set; }
        public IProcessEnvironment GitEnvironment { get; private set; }
        public ICredentialManager CredentialManager { get; private set; }
        public IProcessManager ProcessManager { get; private set; }
        public IKeychain Keychain { get; private set; }
    }
}