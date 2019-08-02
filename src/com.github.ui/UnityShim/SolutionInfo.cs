#pragma warning disable 436

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct("Git for Unity")]
[assembly: AssemblyVersion(System.AssemblyVersionInformation.VersionForAssembly)]
[assembly: AssemblyFileVersion(System.AssemblyVersionInformation.VersionForAssembly)]
[assembly: AssemblyInformationalVersion(System.AssemblyVersionInformation.Version)]
[assembly: ComVisible(false)]
[assembly: AssemblyCompany("Unity Technologies.")]
[assembly: AssemblyCopyright("Copyright Unity Technologies 2019. Copyright GitHub, Inc. 2016-2018")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en-US")]

[assembly: InternalsVisibleTo("TestUtils", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo("UnitTests", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo("IntegrationTests", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo("TaskSystemIntegrationTests", AllInternalsVisible = true)]

//Required for NSubstitute
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2", AllInternalsVisible = true)]

//Required for Unity compilation
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor", AllInternalsVisible = true)]

namespace System
{
    internal static class AssemblyVersionInformation {
        private const string GitForUnityVersion = "2.0.0";
        internal const string VersionForAssembly = GitForUnityVersion;

        // If this is an alpha, beta or other pre-release, mark it as such as shown below
        internal const string Version = GitForUnityVersion + "-alpha.3";
    }
}
