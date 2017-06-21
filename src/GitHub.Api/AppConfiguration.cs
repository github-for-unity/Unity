using System.Reflection;
using Octokit;

namespace GitHub.Unity
{
    class AppConfiguration : IAppConfiguration
    {
        public AppConfiguration()
        {
            var executingAssembly = typeof(AppConfiguration).Assembly;
            AssemblyName = executingAssembly.GetName();
            ProductHeader = new ProductHeaderValue(ApplicationInfo.ApplicationSafeName, AssemblyName.Version.ToString());
        }

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