using GitHub.Unity;
using System;
using NUnit.Framework;

namespace IOTests
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    class TestEnvironment : IEnvironment
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

    [TestFixture]
    public class Tests
    {
        [Test]
        public void BranchListTest()
        {
            var processor = new BranchListOutputProcessor();
            processor.OnBranch += data => Console.WriteLine($"{data.Name} {data.Active} {data.Tracking}");

            var testEnv = new TestEnvironment();
            testEnv.GitInstallPath = @"c:\soft\git";
            var procManager = new ProcessManager(new GitEnvironment(testEnv));
            
            var process = procManager.Configure("git", "branch -vv", @"D:\code\github\UnityInternal");
            var outputManager = new ProcessOutputManager(process, processor);

            process.Run();

            while (!process.WaitForExit(10))
            {

            }

            Console.WriteLine("Exited");
        }
    }
}
