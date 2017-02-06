using GitHub.Unity;
using GitHub.Models;
using Octokit;
using System;
using System.Reflection;
using GitHub.Api;

namespace GitHub.Unity
{
    public class Program : IProgram
    {
        public Program()
        {
            applicationName = ApplicationInfo.ApplicationName;
            applicationDescription = ApplicationInfo.ApplicationDescription;

            var executingAssembly = typeof(Program).Assembly;
            AssemblyName = executingAssembly.GetName();
            ProductHeader = new ProductHeaderValue(ApplicationInfo.ApplicationSafeName, AssemblyName.Version.ToString());
        }

        readonly string applicationName;
        readonly string applicationDescription;

        /// <summary>
        /// Name of this application
        /// </summary>
        public string ApplicationName { get { return applicationName; } }

        /// <summary>
        /// Name of this application
        /// </summary>
        public string ApplicationDescription { get { return applicationDescription; } }

        /// <summary>
        /// The currently executing assembly.
        /// </summary>
        public AssemblyName AssemblyName { get; private set; }

        /// <summary>
        /// The product header used in the user agent.
        /// </summary>
        public ProductHeaderValue ProductHeader { get; private set; }
    }

    class AuthenticationService
    {
        private readonly IProgram program;
        private readonly ICredentialManager credentialManager;
        private ISimpleApiClient client;

        private LoginResult loginResultData;

        public AuthenticationService(IProgram program, ICredentialManager credentialManager)
        {
            this.program = program;
            this.credentialManager = credentialManager;
            var api = new SimpleApiClientFactory(program, credentialManager);
            this.client = api.Create(new UriString("https://github.com/github"));
        }

        public void Login(string username, string password, Action<string> twofaRequired, Action<bool, string> authResult)
        {
            loginResultData = null;
            client.Login(username, password, r =>
            {
                loginResultData = r;
                twofaRequired(r.Message);
            }, authResult);
        }

        public void LoginWith2fa(string code)
        {
            if (loginResultData == null)
                throw new InvalidOperationException("Call Login() first");
            client.ContinueLogin(loginResultData, code);
        }
    }
}
