using System;

namespace GitHub.Unity
{
    interface IEnvironment
    {
        string ExpandEnvironmentVariables(string name);
        string GetEnvironmentVariable(string v);
        string GetSpecialFolder(Environment.SpecialFolder folder);

        NPath Path { get; }
        string NewLine { get; }
        NPath GitExecutablePath { get; set; }
        bool IsWindows { get; }
        bool IsLinux { get; }
        bool IsMac { get; }
        NPath UnityApplication { get; set; }
        NPath UnityAssetsPath { get; set; }
        NPath UnityProjectPath { get; set; }
        NPath ExtensionInstallPath { get; set; }
        NPath UserCachePath { get; set; }
        NPath RepositoryPath { get; }
        NPath GitInstallPath { get; }
        IRepository Repository { get; set; }
        NPath SystemCachePath { get; set; }
    }
}