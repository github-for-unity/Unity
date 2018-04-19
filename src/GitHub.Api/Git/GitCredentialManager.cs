using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitCredentialManager : ICredentialManager
    {
        private static ILogging Logger { get; } = LogHelper.GetLogger<GitCredentialManager>();

        private ICredential credential;
        private string credHelper = null;

        private readonly IProcessManager processManager;
        private readonly ITaskManager taskManager;

        public GitCredentialManager(IProcessManager processManager,
            ITaskManager taskManager)
        {
            this.processManager = processManager;
            this.taskManager = taskManager;
        }

        public bool HasCredentials()
        {
            return credential != null;
        }

        public ICredential CachedCredentials { get { return credential; } }

        public async Task Delete(UriString host)
        {
            //Logger.Trace("Delete: {0}", host);

            if (!await LoadCredentialHelper())
                return;

            await RunCredentialHelper(
                "erase",
                new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                }).StartAwait();
            credential = null;
        }

        public async Task<ICredential> Load(UriString host)
        {
            //Logger.Trace("Load: {0}", host);

            if (credential == null)
            {
                if (!await LoadCredentialHelper())
                    return null;

                string kvpCreds = null;

                kvpCreds = await RunCredentialHelper(
                    "get",
                    new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                    }).StartAwait();

                if (String.IsNullOrEmpty(kvpCreds))
                {
                    Logger.Error("No credentials are stored");
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
                    Logger.Error("No password is stored");
                    return null;
                }

                string user;
                if (!dict.TryGetValue("username", out user))
                {
                    Logger.Error("No username is stored");
                    return null;
                }

                credential = new Credential(host, user, password);
            }
            return credential;
        }

        public async Task Save(ICredential cred)
        {
            this.credential = cred;

            //Logger.Trace("Save: {0}", credential.Host);

            if (!await LoadCredentialHelper())
                return;

            var data = new List<string>
            {
                String.Format("protocol={0}", credential.Host.Protocol),
                String.Format("host={0}", credential.Host.Host),
                String.Format("username={0}", credential.Username),
                String.Format("password={0}", credential.Token)
            };

            var task = RunCredentialHelper("store", data.ToArray());
            await task.StartAwait();
            if (!task.Successful)
            {
                Logger.Error("Failed to save credentials");
            }
        }

        private async Task<bool> LoadCredentialHelper()
        {
            if (credHelper != null)
                return true;

            //Logger.Trace("Loading Credential Helper");

            credHelper = await new GitConfigGetTask("credential.helper", GitConfigSource.NonSpecified, taskManager.Token)
                .Configure(processManager)
                .StartAwait();

            //Logger.Trace("Loaded Credential Helper: {0}", credHelper);

            if (credHelper != null)
            {
                return true;
            }

            Logger.Error("Failed to get the credential helper");
            return false;
        }

        private ITask<string> RunCredentialHelper(string action, string[] lines)
        {
            //Logger.Trace("RunCredentialHelper helper:\"{0}\" action:\"{1}\"", credHelper, action);

            SimpleProcessTask task;
            if (credHelper.StartsWith('!'))
            {
                // it's a separate app, run it as such
                task = new SimpleProcessTask(taskManager.Token, credHelper.Substring(1).ToNPath(), action);
            }
            else
            {
                var args = $"credential-{credHelper} {action}";
                task = new SimpleProcessTask(taskManager.Token, args);
            }

            task.Configure(processManager, true);

            task.OnStartProcess += proc =>
            {
                foreach (var line in lines)
                {
                    proc.StandardInput.WriteLine(line);
                }
                proc.StandardInput.Close();
            };

            return task;
        }
    }
}