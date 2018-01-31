using System.Reflection;
using Octokit;

namespace GitHub.Unity
{
    public static class ApplicationConfiguration
    {
        public const int DefaultWebTimeout = 3000;

        static ApplicationConfiguration()
        {
            var executingAssembly = typeof(ApplicationConfiguration).Assembly;
            AssemblyName = executingAssembly.GetName();
            ProductHeader = new ProductHeaderValue(ApplicationInfo.ApplicationSafeName, AssemblyName.Version.ToString());
        }

        /// <summary>
        ///     The currently executing assembly.
        /// </summary>
        public static AssemblyName AssemblyName { get; }

        /// <summary>
        ///     The product header used in the user agent.
        /// </summary>
        public static ProductHeaderValue ProductHeader { get; private set; }

        public static int WebTimeout { get; set; } = DefaultWebTimeout;
    }
}
