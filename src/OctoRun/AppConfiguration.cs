using GitHub.Unity;
using Octokit;
using System.Reflection;

namespace OctoRun
{
    static class AppConfiguration
    {
        static AppConfiguration()
        {
            var executingAssembly = typeof(AppConfiguration).Assembly;
            AssemblyName = executingAssembly.GetName();
            ProductHeader = new ProductHeaderValue(ApplicationInfo.ApplicationSafeName, AssemblyName.Version.ToString());
        }

        /// <summary>
        /// The currently executing assembly.
        /// </summary>
        public static AssemblyName AssemblyName { get; private set; }

        /// <summary>
        /// The product header used in the user agent.
        /// </summary>
        public static ProductHeaderValue ProductHeader { get; private set; }
    }
}