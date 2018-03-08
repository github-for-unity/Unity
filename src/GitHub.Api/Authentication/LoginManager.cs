using System;
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
        private readonly NPath nodeJsExecutablePath;
        private readonly NPath octorunScript;

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
            IProcessManager processManager = null, ITaskManager taskManager = null, NPath nodeJsExecutablePath = null, NPath octorunScript = null)
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

            string token;
            try
            {
                token = await TryLogin(host, username, password);
                if (string.IsNullOrEmpty(token))
                {
                    throw new InvalidOperationException("Returned token is null or empty");
                }
            }
            catch (TwoFactorRequiredException e)
            {
                LoginResultCodes result;
                result = LoginResultCodes.CodeRequired;
                logger.Trace("2FA TwoFactorAuthorizationException: {0} {1}", LoginResultCodes.CodeRequired, e.Message);

                return new LoginResultData(result, e.Message, host, password);
            }
            catch (Exception e)
            {
                logger.Warning(e, "Login Exception");

                await keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.Failed, Localization.LoginFailed, host);
            }

            keychain.SetToken(host, token);
            await keychain.Save(host);

            return new LoginResultData(LoginResultCodes.Success, "Success", host);
        }

        public async Task<LoginResultData> ContinueLogin(LoginResultData loginResultData, string twofacode)
        {
            var token = loginResultData.Token;
            var host = loginResultData.Host;
            var keychainAdapter = keychain.Connect(host);
            var username = keychainAdapter.Credential.Username;
            var password = keychainAdapter.Credential.Token;
            try
            {
                logger.Trace("2FA Continue");
                token = await TryContinueLogin(host, username, password, twofacode);

                if (string.IsNullOrEmpty(token))
                {
                    throw new InvalidOperationException("Returned token is null or empty");
                }

                keychain.SetToken(host, token);
                await keychain.Save(host);

                return new LoginResultData(LoginResultCodes.Success, "", host);
            }
            catch (Exception e)
            {
                logger.Trace(e, "Exception: {0}", e.Message);

                await keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.Failed, e.Message, host);
            }
        }

        /// <inheritdoc/>
        public async Task Logout(UriString hostAddress)
        {
            Guard.ArgumentNotNull(hostAddress, nameof(hostAddress));

            await new ActionTask(keychain.Clear(hostAddress, true)).StartAwait();
        }

        private async Task<string> TryLogin(
            UriString host,
            string username,
            string password
        )
        {
            var loginTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath, octorunScript,
                "login", ApplicationInfo.ClientId, ApplicationInfo.ClientSecret);
            loginTask.Configure(processManager, workingDirectory: octorunScript.Parent.Parent, withInput: true);
            loginTask.OnStartProcess += proc =>
            {
                proc.StandardInput.WriteLine(username);
                proc.StandardInput.WriteLine(password);
                proc.StandardInput.Close();
            };

            var ret = (await loginTask.StartAwait());

            if (ret.Count == 0)
            {
                throw new Exception("Authentication failed");
            }

            if (ret[0] == "success")
            {
                return ret[1];
            }

            if (ret[0] == "2fa")
            {
                keychain.SetToken(host, ret[1]);
                await keychain.Save(host);
                throw new TwoFactorRequiredException();
            }

            if (ret.Count > 2)
            {
                throw new Exception(ret[3]);
            }

            throw new Exception("Authentication failed");
        }

        private async Task<string> TryContinueLogin(
            UriString host,
            string username,
            string password,
            string code
        )
        {
            logger.Info("Continue Username:{0} {1} {2}", username, password, code);

            var loginTask = new OctorunTask(taskManager.Token, nodeJsExecutablePath, octorunScript,
                "login --twoFactor", ApplicationInfo.ClientId, ApplicationInfo.ClientSecret);
            loginTask.Configure(processManager, workingDirectory: octorunScript.Parent.Parent, withInput: true);
            loginTask.OnStartProcess += proc =>
            {
                proc.StandardInput.WriteLine(username);
                proc.StandardInput.WriteLine(password);
                proc.StandardInput.WriteLine(code);
                proc.StandardInput.Close();
            };

            var ret = (await loginTask.StartAwait());

            logger.Trace("Return: {0}", string.Join(";", ret.ToArray()));

            if (ret.Count == 0)
            {
                throw new Exception("Authentication failed");
            }

            if (ret[0] == "success")
            {
                return ret[1];
            }

            if (ret.Count > 2)
            {
                throw new Exception(ret[3]);
            }

            throw new Exception("Authentication failed");
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

    class TwoFactorRequiredException : Exception
    {
        
    }
}
