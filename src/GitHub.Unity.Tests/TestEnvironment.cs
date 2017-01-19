using System;

namespace GitHub.Unity.Tests
{
    public class TestEnvironment : IEnvironment
    {
        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            return ExpandEnvironmentVariables(Environment.GetFolderPath(folder));
        }

        public string ExpandEnvironmentVariables(string name)
        {
            return Environment.ExpandEnvironmentVariables(name);
        }

        public string GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }

        public string GetTempPath()
        {
            return System.IO.Path.GetTempPath();
        }

        public string UserProfilePath => Environment.GetEnvironmentVariable("USERPROFILE");
        public string Path => Environment.GetEnvironmentVariable("PATH");
        public string NewLine => Environment.NewLine;
        public string GitInstallPath { get; set; }
        public bool IsWindows { get; set; } = true;
    }
}