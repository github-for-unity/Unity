using System.Reflection;
using GitHub.Api;
using Octokit;

namespace GitHub.Unity
{
    class AppConfiguration : IAppConfiguration
    {
        public AppConfiguration()
        {
            applicationName = ApplicationInfo.ApplicationName;
            applicationDescription = ApplicationInfo.ApplicationDescription;

            var executingAssembly = typeof(AppConfiguration).Assembly;
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
}