using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyProduct("GitHub for Unity")]
[assembly: AssemblyVersion(System.AssemblyVersionInformation.Version)]
[assembly: AssemblyFileVersion(System.AssemblyVersionInformation.Version)]
[assembly: ComVisible(false)]
[assembly: AssemblyCompany("GitHub, Inc.")]
[assembly: AssemblyCopyright("Copyright © GitHub, Inc. 2017")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en-US")]

[assembly: InternalsVisibleTo("GitHub.Unity.Tests", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo("GitHub.Api.Tests", AllInternalsVisible = true)]

//Required for NSubstitute
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2", AllInternalsVisible = true)]

//Required for Unity compilation
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor.dll", AllInternalsVisible = true)]

namespace System
{
    internal static class AssemblyVersionInformation {
        internal const string Version = "0.1.0.0";
    }
}
