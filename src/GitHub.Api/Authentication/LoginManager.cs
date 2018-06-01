using System;
using GitHub.Logging;

namespace GitHub.Unity
{
    public enum LoginResultCodes
    {
        Failed,
        Success,
        CodeRequired,
        CodeFailed,
        LockedOut
    }

    /// <summary>
    /// Provides services for logging into a GitHub server.
    /// </summary>
    class LoginManager : ILoginManager
    {
        private readonly ILogging logger = LogHelper.GetLogger<LoginManager>();

        private readonly IKeychain keychain;
        private readonly IProcessManager processManager;
        private readonly ITaskManager taskManager;
        private readonly NPath? nodeJsExecutablePath;
        private readonly NPath? octorunScript;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginManager"/> class.
        /// </summary>
        /// <param name="keychain"></param>
        /// <param name="processManager"></param>
        /// <param name="taskManager"></param>
        /// <param name="nodeJsExecutablePath"></param>
        /// <param name="octorunScript"></param>
        public LoginManager(
            IKeychain keychain, IProcessManager processManager, ITaskManager taskManager,
            NPath? nodeJsExecutablePath = null, NPath? octorunScript = null)
        {
            Guard.ArgumentNotNull(keychain, nameof(keychain));

            this.keychain = keychain;
            this.processManager = processManager;
            this.taskManager = taskManager;
            this.nodeJsExecutablePath = nodeJsExecutablePath;
            this.octorunScript = octorunScript;
        }

        /// <inheritdoc/>
        public LoginResultData Login(
            UriString host,
            string username,
            string password)
        {
            Guard.ArgumentNotNull(host, nameof(host));
            Guard.ArgumentNotNullOrWhiteSpace(username, nameof(username));
            Guard.ArgumentNotNullOrWhiteSpace(password, nameof(password));

            // Start by saving the username and password, these will be used by the `IGitHubClient`
            // until an authorization token has been created and acquired:
            keychain.Connect(host);
            keychain.SetCredentials(new Credential(host, username, password));

            try
            {
                var loginResultData = TryLogin(host, username, password);
                if (loginResultData.Code == LoginResultCodes.Success || loginResultData.Code == LoginResultCodes.CodeRequired)
                {
                    if (string.IsNullOrEmpty(loginResultData.Token))
                    {
                        throw new InvalidOperationException("Returned token is null or empty");
                    }

                    if (loginResultData.Code == LoginResultCodes.Success)
                    {
                        username = RetrieveUsername(loginResultData, username);
                    }

                    keychain.SetToken(host, loginResultData.Token, username);

                    if (loginResultData.Code == LoginResultCodes.Success)
                    {
                        keychain.Save(host);
                    }

                    return loginResultData;
                }

                return loginResultData;
            }
            catch (Exception e)
            {
                logger.Warning(e, "Login Exception");

                keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.Failed, Localization.LoginFailed, host);
            }
        }

        public LoginResultData ContinueLogin(LoginResultData loginResultData, string twofacode)
        {
            var host = loginResultData.Host;
            var keychainAdapter = keychain.Connect(host);
            var username = keychainAdapter.Credential.Username;
            var password = keychainAdapter.Credential.Token;
            try
            {
                loginResultData = TryLogin(host, username, password, twofacode);

                if (loginResultData.Code == LoginResultCodes.Success)
                {
                    if (string.IsNullOrEmpty(loginResultData.Token))
                    {
                        throw new InvalidOperationException("Returned token is null or empty");
                    }

                    username = RetrieveUsername(loginResultData, username);
                    keychain.SetToken(host, loginResultData.Token, username);
                    keychain.Save(host);

                    return loginResultData;
                }

                return loginResultData;
            }
            catch (Exception e)
            {
                logger.Warning(e, "Login Exception");

                keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.Failed, Localization.LoginFailed, host);
            }
        }

        /// <inheritdoc/>
        public ITask Logout(UriString hostAddress)
        {
            Guard.ArgumentNotNull(hostAddress, nameof(hostAddress));
            return taskManager.Run(() => keychain.Clear(hostAddress, true), "Signing out");
        }

        private LoginResultData TryLogin(
            UriString host,
            string username,
            string password,
            string code = null
        )
        {
            if (!nodeJsExecutablePath.HasValue)
            {
                throw new InvalidOperationException("nodeJsExecutablePath must be set");
            }

            if (!octorunScript.HasValue)
            {
                throw new InvalidOperationException("octorunScript must be set");
            }

            var hasTwoFactorCode = code != null;

            var arguments = hasTwoFactorCode ? "login --twoFactor" : "login";
            var loginTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath.Value, octorunScript.Value,
                arguments, ApplicationInfo.ClientId, ApplicationInfo.ClientSecret);
            loginTask.Configure(processManager, workingDirectory: octorunScript.Value.Parent.Parent, withInput: true);
            loginTask.OnStartProcess += proc =>
            {
                proc.StandardInput.WriteLine(username);
                proc.StandardInput.WriteLine(password);
                if (hasTwoFactorCode)
                {
                    proc.StandardInput.WriteLine(code);
                }
                proc.StandardInput.Close();
            };

            var ret = loginTask.RunSynchronously();

            if (ret.IsSuccess)
            {
                return new LoginResultData(LoginResultCodes.Success, null, host, ret.Output[0]);
            }

            if (ret.IsTwoFactorRequired)
            {
                var resultCodes = hasTwoFactorCode ? LoginResultCodes.CodeFailed : LoginResultCodes.CodeRequired;
                var message = hasTwoFactorCode ? "Incorrect code. Two Factor Required." : "Two Factor Required.";

                return new LoginResultData(resultCodes, message, host, ret.Output[0]);
            }

            return new LoginResultData(LoginResultCodes.Failed, ret.GetApiErrorMessage() ?? "Failed.", host);
        }

        private string RetrieveUsername(LoginResultData loginResultData, string username)
        {
            if (!username.Contains("@"))
            {
                return username;
            }

            var octorunTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath.Value, octorunScript.Value, "validate",
                user: username, userToken: loginResultData.Token).Configure(processManager);

            var validateResult = octorunTask.RunSynchronously();
            if (!validateResult.IsSuccess)
            {
                throw new InvalidOperationException("Authentication validation failed");
            }

            return validateResult.Output[1];
        }
    }

    class LoginResultData
    {
        public LoginResultCodes Code;
        public string Message;
        internal string Token { get; set; }
        internal UriString Host { get; set; }

        internal LoginResultData(LoginResultCodes code, string message,
            UriString host, string token)
        {
            this.Code = code;
            this.Message = message;
            this.Token = token;
            this.Host = host;
        }

        internal LoginResultData(LoginResultCodes code, string message, UriString host)
            : this(code, message, host, null)
        {
        }
    }
}
