using System.Reflection;
using Octokit;

namespace GitHub.Unity
{
    // Represents the currently executing program.
    interface IAppConfiguration
    {
        string ApplicationName { get; }
        string ApplicationDescription { get; }
        AssemblyName AssemblyName { get; }
        ProductHeaderValue ProductHeader { get; }
    }
}
