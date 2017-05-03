using GitHub.Unity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitCredentialManager : ICredentialManager
    {
        private static readonly ILogging logger = Logging.GetLogger<GitCredentialManager>();

        private ICredential credential;
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
            return credential != null;
        }

        public ICredential CachedKeys { get { return credential; } }

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

            credential = null;
        }

        public async Task<ICredential> Load(UriString host)
        {
            logger.Trace("Load: {0}", host);

            if (credential == null)
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
                credential = new Credential(host, user, password);
            }
            return credential;
        }

        public async Task Save(ICredential credential)
        {
            logger.Trace("Save: {0}", credential.Host);

            this.credential = credential;

            if (!await LoadCredentialHelper())
                return;

            string result = null;
            var data = new List<string>
            {
                String.Format("protocol={0}", credential.Host.Protocol),
                String.Format("host={0}", credential.Host.Host),
                String.Format("username={0}", credential.Username),
                String.Format("password={0}", credential.Token)
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