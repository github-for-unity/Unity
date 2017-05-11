using System;

namespace GitHub.Unity
{
    interface IEnvironment
    {
        string ExpandEnvironmentVariables(string name);
        string GetEnvironmentVariable(string v);
        string GetSpecialFolder(Environment.SpecialFolder folder);

        string Path { get; }
        string NewLine { get; }
        string GitExecutablePath { get; set; }
        bool IsWindows { get; }
        bool IsLinux { get; }
        bool IsMac { get; }
        string UnityApplication { get; set; }
        string UnityAssetsPath { get; set; }
        string UnityProjectPath { get; set; }
        string ExtensionInstallPath { get; set; }
        NPath UserCachePath { get; set; }
        string RepositoryPath { get; }
        string GitInstallPath { get; }
        IRepository Repository { get; set; }
        NPath SystemCachePath { get; set; }
    }
}