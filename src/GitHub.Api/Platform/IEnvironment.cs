using System;

namespace GitHub.Unity
{
    public interface IEnvironment
    {
        void Initialize(string unityVersion, NPath extensionInstallPath, NPath unityApplicationPath, NPath unityApplicationContentsPath, NPath assetsPath);
        void InitializeRepository(NPath? expectedRepositoryPath = null);
        string ExpandEnvironmentVariables(string name);
        string GetEnvironmentVariable(string v);
        string GetSpecialFolder(Environment.SpecialFolder folder);

        NPath Path { get; }
        string NewLine { get; }
        NPath GitExecutablePath { get; set; }
        NPath NodeJsExecutablePath { get; }
        NPath OctorunScriptPath { get; set; }
        bool IsWindows { get; }
        bool IsLinux { get; }
        bool IsMac { get; }
        string UnityVersion { get; }
        NPath UnityApplication { get; }
        NPath UnityApplicationContents { get; }
        NPath UnityAssetsPath { get; }
        NPath UnityProjectPath { get; }
        NPath ExtensionInstallPath { get; }
        NPath RepositoryPath { get; }
        NPath GitInstallPath { get; }
        NPath UserCachePath { get; set; }
        NPath SystemCachePath { get; set; }
        NPath LogPath { get; }
        IFileSystem FileSystem { get; set; }
        IUser User { get; set; }
        IRepository Repository { get; set; }
        string ExecutableExtension { get; }
        ICacheContainer CacheContainer { get; }
    }
}