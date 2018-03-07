using System;
using System.Net;
using System.Threading.Tasks;
using Octokit;
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

        private readonly string[] scopes = { "user", "repo", "gist", "write:public_key" };
        private readonly IKeychain keychain;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string authorizationNote;
        private readonly string fingerprint;
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
        /// <param name="authorizationNote">An note to store with the authorization.</param>
        /// <param name="fingerprint">The machine fingerprint.</param>
        /// <param name="processManager"></param>
        /// <param name="taskManager"></param>
        /// <param name="nodeJsExecutablePath"></param>
        /// <param name="octorunScript"></param>
        public LoginManager(
            IKeychain keychain,
            string clientId,
            string clientSecret,
            string authorizationNote = null,
            string fingerprint = null,
            IProcessManager processManager = null, ITaskManager taskManager = null, NPath nodeJsExecutablePath = null, NPath octorunScript = null)
        {
            Guard.ArgumentNotNull(keychain, nameof(keychain));
            Guard.ArgumentNotNullOrWhiteSpace(clientId, nameof(clientId));
            Guard.ArgumentNotNullOrWhiteSpace(clientSecret, nameof(clientSecret));

            this.keychain = keychain;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.authorizationNote = authorizationNote;
            this.fingerprint = fingerprint;
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

            var newAuth = new NewAuthorization
            {
                Scopes = scopes,
                Note = authorizationNote,
                Fingerprint = fingerprint,
            };

            ApplicationAuthorization auth = null;

            try
            {
                auth = await TryLogin(host, username, password);
                EnsureNonNullAuthorization(auth);
            }
            catch (TwoFactorAuthorizationException e)
            {
                LoginResultCodes result;
                if (e is TwoFactorRequiredException)
                {
                    result = LoginResultCodes.CodeRequired;
                    logger.Trace("2FA TwoFactorAuthorizationException: {0} {1}", LoginResultCodes.CodeRequired, e.Message);
                }
                else
                {
                    result = LoginResultCodes.CodeFailed;
                    logger.Error(e, "2FA TwoFactorAuthorizationException: {0} {1}", LoginResultCodes.CodeRequired, e.Message);
                }

                return new LoginResultData(result, e.Message, host, newAuth);
            }
            catch(LoginAttemptsExceededException e)
            {
                logger.Warning(e, "Login LoginAttemptsExceededException: {0}", e.Message);

                await keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.LockedOut, Localization.LockedOut, host);
            }
            catch (ApiValidationException e)
            {
                logger.Warning(e, "Login ApiValidationException: {0}", e.Message);

                var message = e.ApiError.FirstErrorMessageSafe();
                await keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.Failed, message, host);
            }
            catch (Exception e)
            {
                logger.Warning(e, "Login Exception");

                // Some enterprise instances don't support OAUTH, so fall back to using the
                // supplied password - on instances that don't support OAUTH the user should
                // be using a personal access token as the password.
                if (EnterpriseWorkaround(host, e))
                {
                    auth = new ApplicationAuthorization(password);
                }
                else
                {
                    await keychain.Clear(host, false);
                    return new LoginResultData(LoginResultCodes.Failed, Localization.LoginFailed, host);
                }
            }

            keychain.SetToken(host, auth.Token);
            await keychain.Save(host);

            return new LoginResultData(LoginResultCodes.Success, "Success", host);
        }

        public async Task<LoginResultData> ContinueLogin(LoginResultData loginResultData, string twofacode)
        {
            var newAuth = loginResultData.NewAuth;
            var host = loginResultData.Host;
            var keychainAdapter = keychain.Connect(host);
            var username = keychainAdapter.Credential.Username;
            var password = keychainAdapter.Credential.Token;
            try
            {
                logger.Trace("2FA Continue");
                var auth = await TryContinueLogin(host, username, password, twofacode);
                
                EnsureNonNullAuthorization(auth);

                keychain.SetToken(host, auth.Token);
                await keychain.Save(host);

                return new LoginResultData(LoginResultCodes.Success, "", host);
            }
            catch (TwoFactorAuthorizationException e)
            {
                logger.Trace(e, "2FA TwoFactorAuthorizationException: {0} {1}", LoginResultCodes.CodeFailed, e.Message);

                return new LoginResultData(LoginResultCodes.CodeFailed, Localization.Wrong2faCode, host, newAuth);
            }
            catch (ApiValidationException e)
            {
                logger.Trace(e, "2FA ApiValidationException: {0}", e.Message);

                var message = e.ApiError.FirstErrorMessageSafe();
                await keychain.Clear(host, false);
                return new LoginResultData(LoginResultCodes.Failed, message, host);
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

        private async Task<ApplicationAuthorization> CreateAndDeleteExistingApplicationAuthorization(
            IGitHubClient client,
            NewAuthorization newAuth,
            string twoFactorAuthenticationCode)
        {
            ApplicationAuthorization result;
            var retry = 0;

            do
            {
                if (twoFactorAuthenticationCode == null)
                {

                    result = await client.Authorization.GetOrCreateApplicationAuthentication(
                        clientId,
                        clientSecret,
                        newAuth);
                }
                else
                {
                    result = await client.Authorization.GetOrCreateApplicationAuthentication(
                        clientId,
                        clientSecret,
                        newAuth,
                        twoFactorAuthenticationCode);
                }

                if (result.Token == string.Empty)
                {
                    if (twoFactorAuthenticationCode == null)
                    {
                        await client.Authorization.Delete(result.Id);
                    }
                    else
                    {
                        await client.Authorization.Delete(result.Id, twoFactorAuthenticationCode);
                    }
                }
            } while (result.Token == string.Empty && retry++ == 0);

            return result;
        }

        private async Task<ApplicationAuthorization> TryLogin(
            UriString host,
            string username,
            string password
        )
        {
            ApplicationAuthorization auth;
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
                auth = new ApplicationAuthorization(ret[1]);
                return auth;
            }

            if (ret[0] == "2fa")
            {
                keychain.SetToken(host, ret[1]);
                await keychain.Save(host);
                throw new TwoFactorRequiredException(TwoFactorType.Unknown);
            }

            if (ret.Count > 2)
            {
                throw new Exception(ret[3]);
            }

            throw new Exception("Authentication failed");
        }

        private async Task<ApplicationAuthorization> TryContinueLogin(
            UriString host,
            string username,
            string password,
            string code
        )
        {
            logger.Info("Continue Username:{0} {1} {2}", username, password, code);

            ApplicationAuthorization auth;
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
                auth = new ApplicationAuthorization(ret[1]);
                return auth;
            }

            if (ret.Count > 2)
            {
                throw new Exception(ret[3]);
            }

            throw new Exception("Authentication failed");
        }

        ApplicationAuthorization EnsureNonNullAuthorization(ApplicationAuthorization auth)
        {
            // If a mock IGitHubClient is not set up correctly, it can return null from
            // IGitHubClient.Authorization.Create - this will cause an infinite loop in Login()
            // so prevent that.
            if (auth == null)
            {
                throw new InvalidOperationException("IGitHubClient.Authorization.Create returned null.");
            }

            return auth;
        }

        bool EnterpriseWorkaround(UriString hostAddress, Exception e)
        {
            // Older Enterprise hosts either don't have the API end-point to PUT an authorization, or they
            // return 422 because they haven't white-listed our client ID. In that case, we just ignore
            // the failure, using basic authentication (with username and password) instead of trying
            // to get an authorization token.
            var apiException = e as ApiException;
            return !HostAddress.IsGitHubDotCom(hostAddress) &&
                (e is NotFoundException ||
                 e is ForbiddenException ||
                 apiException?.StatusCode == (HttpStatusCode)422);
        }
    }

    class LoginResultData
    {
        public LoginResultCodes Code;
        public string Message;
        internal NewAuthorization NewAuth { get; set; }
        internal UriString Host { get; set; }

        internal LoginResultData(LoginResultCodes code, string message,
            UriString host, NewAuthorization newAuth)
        {
            this.Code = code;
            this.Message = message;
            this.NewAuth = newAuth;
            this.Host = host;
        }

        internal LoginResultData(LoginResultCodes code, string message, UriString host)
            : this(code, message, host, null)
        {
        }
    }

}
