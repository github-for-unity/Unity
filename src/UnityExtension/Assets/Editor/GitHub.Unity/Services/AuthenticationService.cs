using GitHub.Api;
using GitHub.Models;
using GitHub.Primitives;
using Octokit;
using System;
using System.Reflection;

namespace GitHub.Unity
{

    public static class ApplicationInfo
    {
#if DEBUG
        public const string ApplicationName = "Unity123";
        public const string ApplicationProvider = "GitHub";
#else
        public const string ApplicationName = "Unity123";
        public const string ApplicationProvider = "GitHub";
#endif
        public const string ApplicationSafeName = "Unity123";
        public const string ApplicationDescription = "GitHub for Unity";
    }

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
        public enum LoginResult
        {
            Ok,
            TwoFA,
            Failed
        }

        public void Login(Action<LoginResult> callback)
        {
            var api = new SimpleApiClientFactory(new Program());
            var client = api.Create(new UriString("https://github.com/github/UnityInternal"));
            var repo = client.GetRepository()
        }
    }
}
