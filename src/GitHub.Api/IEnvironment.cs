using System;

namespace GitHub.Unity
{
    interface IEnvironment
    {
        string ExpandEnvironmentVariables(string name);
        string GetEnvironmentVariable(string v);
        string GetSpecialFolder(Environment.SpecialFolder folder);

        string Path { get; }
        string UserProfilePath { get; }
        string NewLine { get; }
        string GitExecutablePath { get; set; }
        bool IsWindows { get; }
        bool IsLinux { get; }
        bool IsMac { get; }
        string UnityAssetsPath { get; set; }
        string UnityProjectPath { get; set; }
        string ExtensionInstallPath { get; set; }
        string RepositoryPath { get; }
        string GitInstallPath { get; }
        IRepository Repository { get; set; }
    }
}