using GitHub.Unity;
using System;
using System.Threading.Tasks;

namespace GitHub.Api
{
    class WindowsCredentialManager : ICredentialManager
    {
        private static readonly ILogging logger = Logging.GetLogger<WindowsCredentialManager>();

        private ICredential credential;
        private string credHelper = null;

        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;

        public WindowsCredentialManager(IEnvironment environment, IProcessManager processManager)
        {
            this.environment = environment;
            this.processManager = processManager;
        }

        public Task Delete(HostAddress host)
        {
            // TODO: implement credential deletion
            credential = null;
            return TaskEx.FromResult(true);
        }

        public async Task<ICredential> Load(HostAddress host)
        {
            if (credential == null)
            {
                if (!await LoadCredentialHelper())
                    return null;

                string kvpCreds = null;

                var ret = await RunCredentialHelper(
                    "get",
                    new string[] {
                        String.Format("protocol={0}", host.WebUri.Scheme),
                        String.Format("host={0}", host.WebUri.Host)
                    },
                    x => {
                        logger.Debug(x);
                        kvpCreds = x;
                    });


                if (!ret || kvpCreds == null)
                {
                    logger.Error("Failed to get the credential helper");
                    return null;
                }

                if (String.IsNullOrEmpty(kvpCreds))
                {
                    logger.Error("No credentials are stored");
                    return null;
                }

                var entries = kvpCreds.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (entries.Length < 2)
                {
                    logger.Error("Invalid result from credential helper");
                    return null;
                }
                var user = entries[0].Split('=')[1];
                var password = entries[1].Split('=')[1];
                credential = new Credential(host, user, password);
            }
            return credential;
        }

        public async Task Save(ICredential credential)
        {
            this.credential = credential;

            if (!await LoadCredentialHelper())
                return;

            var host = credential.Host;

            string result = null;
            var ret = await RunCredentialHelper(
                "store",
                new string[] {
                        String.Format("protocol={0}", credential.Host.WebUri.Scheme),
                        String.Format("host={0}", credential.Host.WebUri.Host),
                    String.Format("username={0}", credential.Username),
                    String.Format("password={0}", credential.Token)
                },
                x => {
                    logger.Debug(x);
                    result = x;
                });

            if (!ret)
            {
                logger.Error("Failed to save credentials");
            }
        }

        private async Task<bool> LoadCredentialHelper()
        {
            if (credHelper != null)
                return true;

            var task = new GitTask(environment, processManager, null, x => { credHelper = x; }, null);
            task.SetArguments("config --get credential.helper");

            if (await task.RunAsync(new System.Threading.CancellationToken()) && credHelper != null)
            {
                return true;
            }
            logger.Error("Failed to get the credential helper");
            return false;
        }
        private Task<bool> RunCredentialHelper(string action, string[] lines, Action<string> resultCallback)
        {
            ProcessTask task = null;
            string app = null;
            if (credHelper.StartsWith('!'))
            {
                // it's a separate app, run it as such
                task = new ProcessTask(environment, processManager, null, resultCallback, null);
                app = credHelper.Substring(1);
            }
            else
            {
                task = new GitTask(environment, processManager, null, resultCallback, null);
                app = String.Format("credential-{0} ", credHelper);
            }

            task.SetArguments(app + action);
            task.OnCreateProcess += p =>
            {
                p.OnStart += proc =>
                {
                    foreach (var line in lines)
                    {
                        logger.Trace(line);
                        proc.StandardInput.WriteLine(line);
                    }
                    proc.StandardInput.Close();
                };
            };
            return task.RunAsync(new System.Threading.CancellationToken());
        }

    }
}