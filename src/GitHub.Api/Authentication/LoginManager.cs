using System;
using System.Net;
using System.Threading.Tasks;
using Octokit;
using GitHub.Unity;

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
        private static readonly ILogging logger = Logging.GetLogger<LoginManager>();

        private readonly string[] scopes = { "user", "repo", "gist", "write:public_key" };
        private readonly ICredentialManager credentialManager;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string authorizationNote;
        private readonly string fingerprint;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginManager"/> class.
        /// </summary>
        /// <param name="loginCache">The cache in which to store login details.</param>
        /// <param name="twoFactorChallengeHandler">The handler for 2FA challenges.</param>
        /// <param name="clientId">The application's client API ID.</param>
        /// <param name="clientSecret">The application's client API secret.</param>
        /// <param name="authorizationNote">An note to store with the authorization.</param>
        /// <param name="fingerprint">The machine fingerprint.</param>
        public LoginManager(
            ICredentialManager credentialManager,
            string clientId,
            string clientSecret,
            string authorizationNote = null,
            string fingerprint = null)
        {
            Guard.ArgumentNotNull(credentialManager, nameof(credentialManager));
            Guard.ArgumentNotNullOrWhiteSpace(clientId, nameof(clientId));
            Guard.ArgumentNotNullOrWhiteSpace(clientSecret, nameof(clientSecret));

            this.credentialManager = credentialManager;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.authorizationNote = authorizationNote;
            this.fingerprint = fingerprint;
        }

        /// <inheritdoc/>
        public async Task<LoginResultData> Login(
            UriString host,
            IGitHubClient client,
            string username,
            string password)
        {
            Guard.ArgumentNotNull(host, nameof(host));
            Guard.ArgumentNotNull(client, nameof(client));
            Guard.ArgumentNotNullOrWhiteSpace(username, nameof(username));
            Guard.ArgumentNotNullOrWhiteSpace(password, nameof(password));

            var credential = new Credential(host, username, password);

            // Start by saving the username and password, these will be used by the `IGitHubClient`
            // until an authorization token has been created and acquired:
            await credentialManager.Save(credential);

            var newAuth = new NewAuthorization
            {
                Scopes = scopes,
                Note = authorizationNote,
                Fingerprint = fingerprint,
            };

            ApplicationAuthorization auth = null;

            try
            {
                auth = await CreateAndDeleteExistingApplicationAuthorization(client, newAuth, null);
                EnsureNonNullAuthorization(auth);
            }
            catch (TwoFactorAuthorizationException e)
            {
                var result = e is TwoFactorRequiredException ? LoginResultCodes.CodeRequired : LoginResultCodes.CodeFailed;
                return new LoginResultData(result, e.Message, client, host, newAuth);
            }
            catch(LoginAttemptsExceededException)
            {
                await credentialManager.Delete(host);
                return new LoginResultData(LoginResultCodes.LockedOut, Localization.LockedOut, host);
            }
            catch (ApiValidationException e)
            {
                var message = e.ApiError.FirstErrorMessageSafe();
                await credentialManager.Delete(host);
                return new LoginResultData(LoginResultCodes.Failed, message, host);
            }
            catch (Exception e)
            {
                logger.Debug(e);
                // Some enterprise instances don't support OAUTH, so fall back to using the
                // supplied password - on instances that don't support OAUTH the user should
                // be using a personal access token as the password.
                if (EnterpriseWorkaround(host, e))
                {
                    auth = new ApplicationAuthorization(password);
                }
                else
                {
                    await credentialManager.Delete(host);
                    return new LoginResultData(LoginResultCodes.Failed, Localization.LoginFailed, host);
                }
            }

            credential.UpdateToken(auth.Token);
            await credentialManager.Save(credential);
            return new LoginResultData(LoginResultCodes.Success, "Success", host);
        }

        public async Task<LoginResultData> ContinueLogin(LoginResultData loginResultData, string twofacode)
        {
            var client = loginResultData.Client;
            var newAuth = loginResultData.NewAuth;
            var host = loginResultData.Host;

            try
            {
                var auth = await CreateAndDeleteExistingApplicationAuthorization(
                    client,
                    newAuth,
                    twofacode);
                EnsureNonNullAuthorization(auth);

                var credential = await credentialManager.Load(host);
                credential.UpdateToken(auth.Token);
                
                await credentialManager.Save(credential);
                return new LoginResultData(LoginResultCodes.Success, "", host);
            }
            catch (TwoFactorAuthorizationException)
            {
                return new LoginResultData(LoginResultCodes.CodeFailed, Localization.Wrong2faCode, client, host, newAuth);
            }
            catch (ApiValidationException e)
            {
                var message = e.ApiError.FirstErrorMessageSafe();
                await credentialManager.Delete(host);
                return new LoginResultData(LoginResultCodes.Failed, message, host);
            }
        }

        /// <inheritdoc/>
        public async Task Logout(UriString hostAddress, IGitHubClient client)
        {
            Guard.ArgumentNotNull(hostAddress, nameof(hostAddress));
            Guard.ArgumentNotNull(client, nameof(client));

            await credentialManager.Delete(hostAddress);
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
            return !HostAddress.IsGitHubDotComUri(hostAddress.ToUri()) &&
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
        internal IGitHubClient Client { get; set; }

        internal LoginResultData(LoginResultCodes code, string message,
            IGitHubClient client, UriString host, NewAuthorization newAuth)
        {
            this.Code = code;
            this.Message = message;
            this.NewAuth = newAuth;
            this.Host = host;
            this.Client = client;
        }

        internal LoginResultData(LoginResultCodes code, string message, UriString host)
            : this(code, message, null, host, null)
        {
        }
    }

}