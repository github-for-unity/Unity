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
    class Program
    {
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
            string ret = null;
            string error = null;

            int sleepms = 0;
            var p = new OptionSet();
            var readInputToEof = false;
            var lines = new List<string>();
            bool runWebServer = false;
            NPath path = NPath.Default;
            int webServerPort = -1;
            bool generateVersion = false;
            string version = null;
            string url = null;
            string readVersion = null;

            p = p
                .Add("r=", (int v) => retCode = v)
                .Add("d=|data=", v => ret = v)
                .Add("e=|error=", v => error = v)
                .Add("f=|file=", v => ret = File.ReadAllText(v))
                .Add("ef=|errorFile=", v => error = File.ReadAllText(v))
                .Add("s=|sleep=", (int v) => sleepms = v)
                .Add("i|input", v => readInputToEof = true)
                .Add("w|web", v => runWebServer = true)
                .Add("port=", (int v) => webServerPort = v)
                .Add("g|generateVersion", v => generateVersion = true)
                .Add("v=|version=", v => version = v)
                .Add("u=|url=", v => url = v)
                .Add("readVersion=", v => readVersion = v)
                .Add("o=|outfile=", v => path = v.ToNPath().MakeAbsolute())
                .Add("h|help", v => p.WriteOptionDescriptions(Console.Out));

            p.Parse(args);

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
                Logger.Error($"Generating version json {version} to {(path.IsInitialized ? path : "console")}");
                var vv = TheVersion.Parse(version);
                url += $"/unity/releases/github-for-unity-{version}.unitypackage";
                var package = new Package { Url = url, Version = vv};
                var json = package.ToJson(lowerCase: true, onlyPublic: false);
                if (path.IsInitialized)
                    path.WriteAllText(json);
                else
                Logger.Info(json);
                return 0;
            }

            if (runWebServer)
            {
                if (webServerPort < 0)
                    webServerPort = 55555;
                RunWebServer(path, webServerPort);
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

            if (!String.IsNullOrEmpty(ret))
                Console.WriteLine(ret);
            else if (readInputToEof)
                Console.WriteLine(String.Join(Environment.NewLine, lines.ToArray()));

            if (!String.IsNullOrEmpty(error))
                Console.Error.WriteLine(error);

            return retCode;
        }
    }
}
