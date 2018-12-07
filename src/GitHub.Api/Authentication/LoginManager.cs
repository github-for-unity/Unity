using System;
using System.Text;
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
        private readonly IEnvironment environment;

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
            IEnvironment environment)
        {
            Guard.ArgumentNotNull(keychain, nameof(keychain));

            this.keychain = keychain;
            this.processManager = processManager;
            this.taskManager = taskManager;
            this.environment = environment;
        }

        public bool LoginWithToken(UriString host, string token)
        {
            Guard.ArgumentNotNull(host, nameof(host));
            Guard.ArgumentNotNullOrWhiteSpace(token, nameof(token));

            var keychainAdapter = keychain.Connect(host);
            keychainAdapter.Set(new Credential(host, "[token]", token));

            try
            {
                var username = RetrieveUsername(token, host);
                keychainAdapter.Update(token, username);
                keychain.SaveToSystem(host);

                return true;
            }
            catch (Exception e)
            {
                logger.Warning(e, "Login Exception");

                keychain.Clear(host, false);
                return false;
            }
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
            var keychainAdapter = keychain.Connect(host);
            keychainAdapter.Set(new Credential(host, username, password));

            try
            {
                var loginResultData = TryLogin(host, username, password);
                if (loginResultData.Code == LoginResultCodes.Success || loginResultData.Code == LoginResultCodes.CodeRequired)
                {
                    if (string.IsNullOrEmpty(loginResultData.Token))
                    {
                        throw new InvalidOperationException("Returned token is null or empty");
                    }

                    keychainAdapter.Update(loginResultData.Token, username);

                    if (loginResultData.Code == LoginResultCodes.Success)
                    {
                        username = RetrieveUsername(loginResultData.Token, host);
                        keychainAdapter.Update(loginResultData.Token, username);
                        keychain.SaveToSystem(host);
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
            if (keychainAdapter.Credential == null) {
                return new LoginResultData(LoginResultCodes.Failed, Localization.LoginFailed, host);
            }
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

                    keychainAdapter.Update(loginResultData.Token, username);
                    username = RetrieveUsername(loginResultData.Token, host);
                    keychainAdapter.Update(loginResultData.Token, username);
                    keychain.SaveToSystem(host);

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
            var hasTwoFactorCode = code != null;

            var command = new StringBuilder("login");

            if (hasTwoFactorCode)
            {
                command.Append(" --twoFactor");
            }

            if (!HostAddress.IsGitHubDotCom(host))
            {
                command.Append(" -h ");
                command.Append(host.Host);
            }

            var loginTask = new OctorunTask(taskManager.Token, environment, command.ToString());
            loginTask.Configure(processManager, withInput: true);
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

        private string RetrieveUsername(string token, UriString host)
        {
            var command = HostAddress.IsGitHubDotCom(host) ? "validate" : "validate -h " + host.Host;
            var octorunTask = new OctorunTask(taskManager.Token, environment, command, token)
                .Configure(processManager);

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
