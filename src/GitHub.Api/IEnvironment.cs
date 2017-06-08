using System;

namespace GitHub.Unity
{
    interface IEnvironment
    {
        void Initialize(NPath extensionInstallPath, NPath unityPath, NPath assetsPath);
        void Initialize();
        string ExpandEnvironmentVariables(string name);
        string GetEnvironmentVariable(string v);
        string GetSpecialFolder(Environment.SpecialFolder folder);

        string Path { get; }
        string NewLine { get; }
        string GitExecutablePath { get; set; }
        bool IsWindows { get; }
        bool IsLinux { get; }
        bool IsMac { get; }
        NPath UnityApplication { get; }
        NPath UnityAssetsPath { get; }
        NPath UnityProjectPath { get; }
        NPath ExtensionInstallPath { get; }
        string RepositoryPath { get; }
        string GitInstallPath { get; }
        IRepository Repository { get; set; }
        NPath UserCachePath { get; set; }
        NPath SystemCachePath { get; set; }
        NPath LogPath { get; }
        IFileSystem FileSystem { get; set; }
    }
}