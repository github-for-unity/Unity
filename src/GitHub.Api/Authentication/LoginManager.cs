using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly IProcessManager processManager;
        private readonly ITaskManager taskManager;
        private readonly NPath? nodeJsExecutablePath;
        private readonly NPath? octorunScript;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginManager"/> class.
        /// </summary>
        /// <param name="keychain"></param>
        /// <param name="clientId">The application's client API ID.</param>
        /// <param name="clientSecret">The application's client API secret.</param>
        /// <param name="processManager"></param>
        /// <param name="taskManager"></param>
        /// <param name="nodeJsExecutablePath"></param>
        /// <param name="octorunScript"></param>
        public LoginManager(
            IKeychain keychain,
            string clientId,
            string clientSecret,
            IProcessManager processManager = null, ITaskManager taskManager = null, NPath? nodeJsExecutablePath = null, NPath? octorunScript = null)
        {
            Guard.ArgumentNotNull(keychain, nameof(keychain));
            Guard.ArgumentNotNullOrWhiteSpace(clientId, nameof(clientId));
            Guard.ArgumentNotNullOrWhiteSpace(clientSecret, nameof(clientSecret));

            this.keychain = keychain;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.processManager = processManager;
            this.taskManager = taskManager;
            this.nodeJsExecutablePath = nodeJsExecutablePath;
            this.octorunScript = octorunScript;
        }

        /// <inheritdoc/>
        public async Task<LoginResultData> Login(
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
                var loginResultData = await TryLogin(host, username, password);
                if (loginResultData.Code == LoginResultCodes.Success || loginResultData.Code == LoginResultCodes.CodeRequired)
                {
                    if (string.IsNullOrEmpty(loginResultData.Token))
                    {
                        throw new InvalidOperationException("Returned token is null or empty");
                    }

                    keychain.SetToken(host, loginResultData.Token);

                    if (loginResultData.Code == LoginResultCodes.Success)
                    {
                        await keychain.Save(host);
                    }

                    return loginResultData;
                }

                return loginResultData;
            }
            catch (Exception e)
            {
                logger.Warning(e, "Login Exception");

                await keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.Failed, Localization.LoginFailed, host);
            }
        }

        public async Task<LoginResultData> ContinueLogin(LoginResultData loginResultData, string twofacode)
        {
            var host = loginResultData.Host;
            var keychainAdapter = keychain.Connect(host);
            var username = keychainAdapter.Credential.Username;
            var password = keychainAdapter.Credential.Token;
            try
            {
                logger.Trace("2FA Continue");
                loginResultData = await TryLogin(host, username, password, twofacode);

                if (loginResultData.Code == LoginResultCodes.Success)
                {
                    if (string.IsNullOrEmpty(loginResultData.Token))
                    {
                        throw new InvalidOperationException("Returned token is null or empty");
                    }

                    keychain.SetToken(host, loginResultData.Token);
                    await keychain.Save(host);

                    return loginResultData;
                }

                return loginResultData;
            }
            catch (Exception e)
            {
                logger.Warning(e, "Login Exception");

                await keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.Failed, Localization.LoginFailed, host);
            }
        }

        /// <inheritdoc/>
        public async Task Logout(UriString hostAddress)
        {
            Guard.ArgumentNotNull(hostAddress, nameof(hostAddress));

            await new ActionTask(keychain.Clear(hostAddress, true)).StartAwait();
        }

        private async Task<LoginResultData> TryLogin(
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

            var ret = await loginTask.StartAwait();

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
