using GitHub.Unity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitCredentialManager : IKeychainManager
    {
        private static readonly ILogging logger = Logging.GetLogger<GitCredentialManager>();

        private IKeychainItem keychainItem;
        private string credHelper = null;

        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;

        public GitCredentialManager(IEnvironment environment, IProcessManager processManager)
        {
            this.environment = environment;
            this.processManager = processManager;
        }

        public bool HasCredentials()
        {
            return keychainItem != null;
        }

        public IKeychainItem CachedKeys { get { return keychainItem; } }

        public async Task Delete(UriString host)
        {
            logger.Trace("Delete: {0}", host);

            if (!await LoadCredentialHelper())
                return;

            var ret = await RunCredentialHelper(
                "erase",
                new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                });

            keychainItem = null;
        }

        public async Task<IKeychainItem> Load(UriString host)
        {
            logger.Trace("Load: {0}", host);

            if (keychainItem == null)
            {
                if (!await LoadCredentialHelper())
                    return null;

                string kvpCreds = null;

                var ret = await RunCredentialHelper(
                    "get",
                    new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                    },
                    x => {
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
                var dict = new Dictionary<string, string>();
                foreach (var entry in entries)
                {
                    var parts = entry.Split('=');
                    dict.Add(parts[0], parts[1]);
                }

                string password = null;
                if (!dict.TryGetValue("password", out password))
                {
                    logger.Error("No password is stored");
                    return null;
                }

                string user = null;
                dict.TryGetValue("user", out user);
                keychainItem = new KeychainItem(host, user, password);
            }
            return keychainItem;
        }

        public async Task Save(IKeychainItem keychainItem)
        {
            logger.Trace("Save: {0}", keychainItem.Host);

            this.keychainItem = keychainItem;

            if (!await LoadCredentialHelper())
                return;

            string result = null;
            var data = new List<string>
            {
                String.Format("protocol={0}", keychainItem.Host.Protocol),
                String.Format("host={0}", keychainItem.Host.Host),
                String.Format("username={0}", keychainItem.Username),
                String.Format("password={0}", keychainItem.Token)
            };

            var ret = await RunCredentialHelper(
                "store",
                data.ToArray(),
                x => {
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

            logger.Trace("Loading Credential Helper");

            var task = new GitConfigGetTask(environment, processManager,
                TaskResultDispatcher.Default.GetDispatcher<string>(x =>
                {
                    logger.Trace("Loaded Credential Helper: {0}", x);
                    credHelper = x;
                }),
                "credential.helper", GitConfigSource.NonSpecified);

            if (await task.RunAsync(processManager.CancellationToken) && credHelper != null)
            {
                return true;
            }

            logger.Error("Failed to get the credential helper");
            return false;
        }
        private Task<bool> RunCredentialHelper(string action, string[] lines, Action<string> resultCallback = null)
        {
            ProcessTask task = null;
            string app = "";
            if (credHelper.StartsWith('!'))
            {
                // it's a separate app, run it as such
                task = new ProcessTask(environment, processManager, TaskResultDispatcher.Default.GetDispatcher(resultCallback), credHelper.Substring(1));
            }
            else
            {
                task = new GitTask(environment, processManager, TaskResultDispatcher.Default.GetDispatcher(resultCallback));
                app = String.Format("credential-{0} ", credHelper);
            }

            task.SetArguments(app + action);
            task.OnCreateProcess += p =>
            {
                p.OnStart += proc =>
                {
                    foreach (var line in lines)
                    {
                        proc.StandardInput.WriteLine(line);
                    }
                    proc.StandardInput.Close();
                };
            };
            return task.RunAsync(processManager.CancellationToken);
        }

    }
}