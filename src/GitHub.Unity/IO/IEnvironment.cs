namespace GitHub.Unity
{
    public interface IEnvironment
    {
        string ExpandEnvironmentVariables(string name);
        string GetEnvironmentVariable(string v);
        string GetTempPath();

        string Path { get; }
        string UserProfilePath { get; }
        string NewLine { get; }
        string GitInstallPath { get; set; }
        bool IsWindows { get; set; }
    }
}