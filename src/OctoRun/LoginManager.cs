using System;
using System.Net;
using System.Threading.Tasks;
using GitHub.Unity;
using Octokit;
using System.Diagnostics;
using GitHub.Logging;

namespace OctoRun
{
    class LoginManager
    {
        private readonly ILogging logger = LogHelper.GetLogger<LoginManager>();

        private readonly string[] scopes = { "user", "repo", "gist", "write:public_key" };
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string authorizationNote;
        private readonly string fingerprint;
        private readonly IKeychain keychain;

        public LoginManager(
            IKeychain keychain,
            string clientId,
            string clientSecret,
            string authorizationNote = null,
            string fingerprint = null)
        {
            Guard.ArgumentNotNull(keychain, nameof(keychain));
            Guard.ArgumentNotNullOrWhiteSpace(clientId, nameof(clientId));
            Guard.ArgumentNotNullOrWhiteSpace(clientSecret, nameof(clientSecret));

            this.keychain = keychain;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.authorizationNote = authorizationNote;
            this.fingerprint = fingerprint;
        }

        public LoginResultData Login(IGitHubClient client)
        {
            Guard.ArgumentNotNull(client, nameof(client));

            var newAuth = new NewAuthorization
            {
                Scopes = scopes,
                Note = authorizationNote,
                Fingerprint = fingerprint,
            };

            ApplicationAuthorization auth = null;

            try
            {
                try
                {
                    logger.Info("Login Username:{0}", keychain.Login);

                    auth = CreateAndDeleteExistingApplicationAuthorization(client, newAuth, null).Result;
                    EnsureNonNullAuthorization(auth);
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }
            }
            catch (TwoFactorAuthorizationException e)
            {
                LoginResultCodes result;
                if (e is TwoFactorRequiredException)
                {
                    result = LoginResultCodes.CodeRequired;
                    logger.Trace("2FA TwoFactorAuthorizationException: {0} {1}", LoginResultCodes.CodeRequired, "2fa");
                }
                else
                {
                    result = LoginResultCodes.CodeFailed;
                    logger.Error(e, "2FA TwoFactorAuthorizationException: {0} {1}", LoginResultCodes.CodeRequired, "wrong");
                }

                return new LoginResultData(result, e.Message, client, newAuth);
            }
            catch (LoginAttemptsExceededException e)
            {
                logger.Warning(e, "Login LoginAttemptsExceededException: {0}", e.Message);

                return new LoginResultData(LoginResultCodes.LockedOut, "locked");
            }
            catch (ApiValidationException e)
            {
                logger.Warning(e, "Login ApiValidationException: {0}", e.Message);

                var message = e.ApiError.FirstErrorMessageSafe();
                return new LoginResultData(LoginResultCodes.Failed, message);
            }
            catch (Exception e)
            {
                logger.Warning(e, "Login Exception");

                // Some enterprise instances don't support OAUTH, so fall back to using the
                // supplied password - on instances that don't support OAUTH the user should
                // be using a personal access token as the password.
                if (EnterpriseWorkaround(client.Connection.BaseAddress.ToUriString(), e))
                {
                    auth = new ApplicationAuthorization(keychain.Token);
                }
                else
                {
                    return new LoginResultData(LoginResultCodes.Failed, "failed");
                }
            }

            keychain.Token = auth.Token;

            return new LoginResultData(LoginResultCodes.Success, auth.Token);
        }


        public LoginResultData ContinueLogin(IGitHubClient client)
        {
            var twofacode = keychain.Code;
            var newAuth = new NewAuthorization
            {
                Scopes = scopes,
                Note = authorizationNote,
                Fingerprint = fingerprint,
            };

            try
            {
                logger.Trace("2FA Continue");

                var auth = CreateAndDeleteExistingApplicationAuthorization(
                    client,
                    newAuth,
                    twofacode).Result;
                EnsureNonNullAuthorization(auth);

                keychain.Token = auth.Token;

                return new LoginResultData(LoginResultCodes.Success, "");
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
            catch (TwoFactorAuthorizationException e)
            {
                logger.Trace(e, "2FA TwoFactorAuthorizationException: {0} {1}", LoginResultCodes.CodeFailed, e.Message);

                return new LoginResultData(LoginResultCodes.CodeFailed, "wrong code", client, newAuth);
            }
            catch (ApiValidationException e)
            {
                logger.Trace(e, "2FA ApiValidationException: {0}", e.Message);

                var message = e.ApiError.FirstErrorMessageSafe();
                return new LoginResultData(LoginResultCodes.Failed, message);
            }
            catch (Exception e)
            {
                logger.Trace(e, "Exception: {0}", e.Message);

                return new LoginResultData(LoginResultCodes.Failed, e.Message);
            }
        }

        private async Task<ApplicationAuthorization> CreateAndDeleteExistingApplicationAuthorization(
            IGitHubClient client,
            NewAuthorization newAuth,
            string twoFactorAuthenticationCode)
        {
            ApplicationAuthorization result = null;
            var retry = 0;

            do
            {
                try
                {
                    if (twoFactorAuthenticationCode == null)
                    {
                        result = client.Authorization.GetOrCreateApplicationAuthentication(
                            clientId,
                            clientSecret,
                            newAuth).Result;
                    }
                    else
                    {
                        result = client.Authorization.GetOrCreateApplicationAuthentication(
                            clientId,
                            clientSecret,
                            newAuth,
                            twoFactorAuthenticationCode).Result;
                    }
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }
                catch (Exception ex)
                {
                    throw ex;
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
            return !HostAddress.IsGitHubDotCom(hostAddress) &&
                (e is NotFoundException ||
                    e is ForbiddenException ||
                    apiException?.StatusCode == (HttpStatusCode)422);
        }

        public enum LoginResultCodes
        {
            Failed,
            Success,
            CodeRequired,
            CodeFailed,
            LockedOut
        }

        public class LoginResult
        {
            public bool NeedTwoFA { get { return Data.Code == LoginResultCodes.CodeRequired || Data.Code == LoginResultCodes.CodeFailed; } }
            public bool Success { get { return Data.Code == LoginResultCodes.Success; } }
            public bool Failed { get { return Data.Code == LoginResultCodes.Failed; } }
            public string Message { get { return Data.Message; } }

            internal LoginResultData Data { get; set; }
            internal Action<LoginResult> Callback { get; set; }
            internal Action<LoginResult, string> TwoFACallback { get; set; }

            internal LoginResult(LoginResultData data, Action<LoginResult> callback = null, Action<LoginResult, string> twofaCallback = null)
            {
                this.Data = data;
                this.Callback = callback;
                this.TwoFACallback = twofaCallback;
            }
        }

        public class LoginResultData
        {
            public LoginResultCodes Code;
            public string Message;
            internal NewAuthorization NewAuth { get; set; }
            internal IGitHubClient Client { get; set; }

            internal LoginResultData(LoginResultCodes code, string message,
                IGitHubClient client, NewAuthorization newAuth)
            {
                this.Code = code;
                this.Message = message;
                this.NewAuth = newAuth;
                this.Client = client;
            }

            internal LoginResultData(LoginResultCodes code, string message)
                : this(code, message, null, null)
            {
            }
        }
    }
}