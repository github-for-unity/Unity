using System;
using System.Collections.Generic;
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
        private readonly ITaskManager taskManager;

        public GitCredentialManager(IEnvironment environment, IProcessManager processManager,
            ITaskManager taskManager)
        {
            this.environment = environment;
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
            logger.Trace("Delete: {0}", host);

            if (!await LoadCredentialHelper())
                return;

            await RunCredentialHelper(
                "erase",
                new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                }).Task;
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

                kvpCreds = await RunCredentialHelper(
                    "get",
                    new string[] {
                        String.Format("protocol={0}", host.Protocol),
                        String.Format("host={0}", host.Host)
                    }).Task;

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

            var data = new List<string>
            {
                String.Format("protocol={0}", credential.Host.Protocol),
                String.Format("host={0}", credential.Host.Host),
                String.Format("username={0}", credential.Username),
                String.Format("password={0}", credential.Token)
            };

            var task = RunCredentialHelper("store", data.ToArray());
            await task.Task;
            if (!task.Successful)
            {
                logger.Error("Failed to save credentials");
            }
        }

        private async Task<bool> LoadCredentialHelper()
        {
            if (credHelper != null)
                return true;

            logger.Trace("Loading Credential Helper");

            credHelper = await new GitConfigGetTask("credential.helper", GitConfigSource.NonSpecified, taskManager.Token).Schedule(taskManager).Task;

            if (credHelper != null)
            {
                return true;
            }

            logger.Error("Failed to get the credential helper");
            return false;
        }

        private ITask<string> RunCredentialHelper(string action, string[] lines)
        {
            ITask<string> task = null;
            string app = "";
            if (credHelper.StartsWith('!'))
            {

                // it's a separate app, run it as such
                task = new ProcessTask<string>(taskManager.Token, new SimpleOutputProcessor())
                    .Configure(processManager, credHelper.Substring(1), action, null, true);
            }
            else
            {
                app = String.Format("credential-{0} ", credHelper);
                task = new ProcessTask<string>(taskManager.Token, app, new SimpleOutputProcessor())
                    .Configure(processManager, true);
            }

            task.OnStart += t =>
            {
                var proc = ((IProcess)t).Process;
                foreach (var line in lines)
                {
                    proc.StandardInput.WriteLine(line);
                }
                proc.StandardInput.Close();
            };

            return task.Schedule(taskManager);
        }
    }
}