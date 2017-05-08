using System.Threading.Tasks;

namespace GitHub.Unity
{
    class Platform : IPlatform
    {
        public Platform(IEnvironment environment, IFileSystem filesystem, IUIDispatcher uiDispatcher)
        {
            Environment = environment;
            UIDispatcher = uiDispatcher;

            NPath localAppData;
            NPath commonAppData;
            if (environment.IsWindows)
            {
                GitEnvironment = new WindowsGitEnvironment(environment, filesystem);
                localAppData = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).ToNPath();
                commonAppData = Environment.GetSpecialFolder(System.Environment.SpecialFolder.CommonApplicationData).ToNPath();
            }
            else if (environment.IsMac)
            {
                GitEnvironment = new MacGitEnvironment(environment, filesystem);
                localAppData = NPath.HomeDirectory.Combine("Library", "Application Support");
                // there is no such thing on the mac that is guaranteed to be user accessible (/usr/local might not be)
                commonAppData = Environment.GetSpecialFolder(System.Environment.SpecialFolder.ApplicationData).ToNPath();
            }
            else
            {
                GitEnvironment = new LinuxGitEnvironment(environment, filesystem);
                localAppData = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).ToNPath();
                commonAppData = "/usr/local/share/";
            }

            Environment.UserCachePath = localAppData.Combine(ApplicationInfo.ApplicationName);
            Environment.SystemCachePath = commonAppData.Combine(ApplicationInfo.ApplicationName);
        }

        public Task<IPlatform> Initialize(IEnvironment environment, IProcessManager processManager)
        {
            ProcessManager = processManager;

            if (CredentialManager == null)
            {
                CredentialManager = new GitCredentialManager(Environment, processManager);
                Keychain = new Keychain(environment, CredentialManager);
                Keychain.Initialize();
            }

            return TaskEx.FromResult(this as IPlatform);
        }

        public IEnvironment Environment { get; private set; }
        public IProcessEnvironment GitEnvironment { get; private set; }
        public ICredentialManager CredentialManager { get; private set; }
        public IProcessManager ProcessManager { get; private set; }
        public IUIDispatcher UIDispatcher { get; private set; }
        public IKeychain Keychain { get; private set; }
    }
}