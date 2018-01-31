using System.Reflection;
using Octokit;

namespace GitHub.Unity
{
    public static class ApplicationConfiguration
    {
        static ApplicationConfiguration()
        {
            var executingAssembly = typeof(ApplicationConfiguration).Assembly;
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

        private static int webTimeout = 3000;
        public static int WebTimeout
        {
            get { return webTimeout; }
            set { webTimeout = value; }
        }
    }
}