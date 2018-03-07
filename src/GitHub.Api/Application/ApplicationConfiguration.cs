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
        }

        /// <summary>
        ///     The currently executing assembly.
        /// </summary>
        public static AssemblyName AssemblyName { get; }

        public static int WebTimeout { get; set; } = DefaultWebTimeout;
    }
}
