using System;
using System.Collections.Generic;
using Mono.Options;
using System.IO;
using System.Threading;
using GitHub.Unity;
using GitHub;
using GitHub.Logging;

namespace TestApp
{
    static class Program
    {
        private static string ReadAllTextIfFileExists(this string path)
        {
            var file = path.ToNPath();
            if (!file.IsInitialized || !file.FileExists())
                return null;
            return file.ReadAllText();
        }

        private static ILogging Logger = LogHelper.GetLogger();

        static void RunWebServer(NPath path, int port)
        {
            if (!path.IsInitialized)
            {
                path = typeof(Program).Assembly.Location.ToNPath().Parent.Combine("files");
            }

            var server = new TestWebServer.HttpServer(path, port);
            var thread = new Thread(() =>
            {
                Logger.Error($"Press any key to exit");
                server.Start();
            });
            thread.Start();
            Console.Read();
            server.Stop();
        }

        static int Main(string[] args)
        {
            LogHelper.LogAdapter = new ConsoleLogAdapter();

            int retCode = 0;
            string data = null;
            string error = null;
            int sleepms = 0;
            var p = new OptionSet();
            var readInputToEof = false;
            var lines = new List<string>();
            bool runWebServer = false;
            NPath outfile = NPath.Default;
            NPath path = NPath.Default;
            string releaseNotes = null;
            int webServerPort = -1;
            bool generateVersion = false;
            bool generatePackage = false;
            string version = null;
            string url = null;
            string execMd5 = null;
            string readVersion = null;
            string msg = null;


            p = p
                .Add("r=", (int v) => retCode = v)
                .Add("d=|data=", v => data = v)
                .Add("e=|error=", v => error = v)
                .Add("f=|file=", v => data = File.ReadAllText(v))
                .Add("ef=|errorFile=", v => error = File.ReadAllText(v))
                .Add("s=|sleep=", (int v) => sleepms = v)
                .Add("i|input", v => readInputToEof = true)
                .Add("w|web", v => runWebServer = true)
                .Add("port=", (int v) => webServerPort = v)
                .Add("g|generateVersion", v => generateVersion = true)
                .Add("v=|version=", v => version = v)
                .Add("p|gen-package", "Pass --version --url --path --md5 --rn --msg to generate a package", v => generatePackage = true)
                .Add("u=|url=", v => url = v)
                .Add("path=", v => path = v.ToNPath())
                .Add("md5=", v => execMd5 = v)
                .Add("rn=", "Path to file with release notes", v => releaseNotes = v.ReadAllTextIfFileExists())
                .Add("msg=", "Path to file with message for package", v => msg = v.ReadAllTextIfFileExists())
                .Add("readVersion=", v => readVersion = v)
                .Add("o=|outfile=", v => outfile = v.ToNPath().MakeAbsolute())
                .Add("h|help", v => p.WriteOptionDescriptions(Console.Out));

            p.Parse(args);

            if (generatePackage)
            {
                var md5 = path.CalculateMD5();
                var package = new Package
                {
                    ExecutableMd5 = execMd5,
                    Message = msg,
                    Md5 = md5,
                    ReleaseNotes = releaseNotes,
                    ReleaseNotesUrl = null,
                    Url = url,
                    Version = TheVersion.Parse(version),
                };
                var json = package.ToJson(lowerCase: true, onlyPublic: false);
                if (outfile.IsInitialized)
                    outfile.WriteAllText(json);
                else
                    Logger.Info(json);
                return 0;
            }

            if (readVersion != null)
            {
                var json = File.ReadAllText(readVersion);
                var package = json.FromJson<Package>(lowerCase: true, onlyPublic: false);
                Console.WriteLine(package);
                Console.WriteLine($"{package.Url} {package.Version}");
                return 0;
            }

            if (generateVersion)
            {
                Logger.Error($"Generating version json {version} to {(outfile.IsInitialized ? outfile : "console")}");
                var vv = TheVersion.Parse(version);
                url += $"/unity/releases/github-for-unity-{version}.unitypackage";
                var package = new Package { Url = url, Version = vv};
                var json = package.ToJson(lowerCase: true, onlyPublic: false);
                if (outfile.IsInitialized)
                    outfile.WriteAllText(json);
                else
                    Logger.Info(json);
                return 0;
            }

            if (runWebServer)
            {
                if (webServerPort < 0)
                    webServerPort = 50000;
                RunWebServer(outfile, webServerPort);
                return 0;
            }

            if (readInputToEof)
            {
                string line;
                while ((line = Console.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            if (sleepms > 0)
                Thread.Sleep(sleepms);

            if (!String.IsNullOrEmpty(data))
                Console.WriteLine(data);
            else if (readInputToEof)
                Console.WriteLine(String.Join(Environment.NewLine, lines.ToArray()));

            if (!String.IsNullOrEmpty(error))
                Console.Error.WriteLine(error);

            return retCode;
        }
    }
}
