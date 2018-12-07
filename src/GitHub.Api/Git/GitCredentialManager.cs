using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    class GitCredentialManager : ICredentialManager
    {
        private static ILogging Logger { get; } = LogHelper.GetLogger<GitCredentialManager>();

        private string credHelper = null;

        private readonly IProcessManager processManager;
        private readonly ITaskManager taskManager;
        private readonly Dictionary<string, ICredential> credentials = new Dictionary<string, ICredential>();

        public GitCredentialManager(IProcessManager processManager,
            ITaskManager taskManager)
        {
            this.processManager = processManager;
            this.taskManager = taskManager;
        }

        public bool HasCredentials()
        {
            return credentials != null && credentials.Any();
        }

        public void Delete(UriString host)
        {
            if (!LoadCredentialHelper())
                return;

            RunCredentialHelper(
                "erase",
                new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                }).RunSynchronously();
            credentials.Remove(host);
        }

        public ICredential Load(UriString host)
        {
            ICredential credential;
            if (!credentials.TryGetValue(host, out credential))
            {
                if (!LoadCredentialHelper())
                    return null;

                string kvpCreds = null;

                kvpCreds = RunCredentialHelper(
                    "get",
                    new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                    }).RunSynchronously();

                if (String.IsNullOrEmpty(kvpCreds))
                {
                    // we didn't find credentials, stop here
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
                credentials.Add(host, credential);
            }

            return credential;
        }

        public void Save(ICredential cred)
        {
            this.credentials.Add(cred.Host, cred);

            if (!LoadCredentialHelper())
                return;

            var data = new List<string>
            {
                String.Format("protocol={0}", cred.Host.Protocol),
                String.Format("host={0}", cred.Host.Host),
                String.Format("username={0}", cred.Username),
                String.Format("password={0}", cred.Token)
            };

            var task = RunCredentialHelper("store", data.ToArray());
            task.RunSynchronously();
            if (!task.Successful)
            {
                Logger.Error("Failed to save credentials");
            }
        }

        private bool LoadCredentialHelper()
        {
            if (credHelper != null)
                return true;

            credHelper = new GitConfigGetTask("credential.helper", GitConfigSource.NonSpecified, taskManager.Token)
                .Configure(processManager)
                .RunSynchronously();

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
