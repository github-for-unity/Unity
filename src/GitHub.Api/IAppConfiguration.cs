using System.Reflection;
using Octokit;

namespace GitHub.Unity
{
    // Represents the currently executing program.
    interface IAppConfiguration
    {
        AssemblyName AssemblyName { get; }
        ProductHeaderValue ProductHeader { get; }
    }
}
