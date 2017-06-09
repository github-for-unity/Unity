using System;
using System.Collections.Generic;
using Mono.Options;
using System.IO;
using System.Threading;

namespace TestApp
{
    class Program
    {
        static int Main(string[] args)
        {
            int retCode = 0;
            string ret = null;
            string error = null;

            int sleepms = 0;
            var p = new OptionSet();
            var readInputToEof = false;
            var lines = new List<string>();

            p = p
                .Add("r=", (int v) => retCode = v)
                .Add("d=|data=", v => ret = v)
                .Add("e=|error=", v => error = v)
                .Add("f=|file=", v => ret = File.ReadAllText(v))
                .Add("ef=|errorFile=", v => error = File.ReadAllText(v))
                .Add("s=|sleep=", (int v) => sleepms = v)
                .Add("i|input", v => readInputToEof = true)
                .Add("h|help", v => p.WriteOptionDescriptions(Console.Out));

            p.Parse(args);

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
