#pragma warning disable 436

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct("GitHub for Unity")]
[assembly: AssemblyVersion(System.AssemblyVersionInformation.Version)]
[assembly: AssemblyFileVersion(System.AssemblyVersionInformation.Version)]
[assembly: AssemblyInformationalVersion(System.AssemblyVersionInformation.Version)]
[assembly: ComVisible(false)]
[assembly: AssemblyCompany("GitHub, Inc.")]
[assembly: AssemblyCopyright("Copyright GitHub, Inc. 2017-2018")]
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
        internal const string Version = "0.30.2";
    }
}
