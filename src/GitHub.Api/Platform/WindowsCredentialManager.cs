using GitHub.Unity;
using System;
using System.Collections.Generic;
using System.Threading;
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

        public async Task Delete(UriString host)
        {
            // TODO: implement credential deletion
            var ret = await RunCredentialHelper(
                "erase",
                new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                },
                x => {
                    logger.Debug(x);
                });

            credential = null;
        }

        public async Task<ICredential> Load(UriString host)
        {
            if (credential == null)
            {
                if (!await LoadCredentialHelper())
                    return null;

                if (credHelper == "manager")
                {
                    // disable the prompt on gcm, we're handling it on this repo
                    var args = String.Format("config --system credential.{0}.interactive never", host.Host);
                    await GitTask.Run(environment, processManager, args);
                    args = String.Format("config --system credential.{0}.validate false", host.Host);
                    await GitTask.Run(environment, processManager, args);
                    args = String.Format("config --system credential.{0}.modalPrompt false", host.Host);
                }

                string kvpCreds = null;

                var ret = await RunCredentialHelper(
                    "get",
                    new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
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
            this.credential = credential;

            if (!await LoadCredentialHelper())
                return;

            var host = credential.Host;

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

            var task = new GitConfigGetTask(environment, processManager, null,
                    "credential.helper", GitConfigSource.NonSpecified,
                    x => {
                        credHelper = x;
                        logger.Trace("credHelper: {0}", credHelper);
                    }, null);

            if (await task.RunAsync(processManager.CancellationToken) && credHelper != null)
            {
                return true;
            }

            logger.Error("Failed to get the credential helper");
            return false;
        }
        private Task<bool> RunCredentialHelper(string action, string[] lines, Action<string> resultCallback)
        {
            ProcessTask task = null;
            string app = "";
            if (credHelper.StartsWith('!'))
            {
                // it's a separate app, run it as such
                task = new ProcessTask(environment, processManager, credHelper.Substring(1), resultCallback);
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
            return task.RunAsync(processManager.CancellationToken);
        }

    }
}