using GitHub.Unity;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static OctoRun.LoginManager;

namespace OctoRun
{
    class LoginCommand
    {
        public Command[] Commands { get; private set; }
        private string host;
        private bool in2fa;

        public static LoginCommand Initialize()
        {
            var instance = new LoginCommand();
            instance.Commands = new Command[]
            {
                new Command("login", "login")
                {
                    Options = new OptionSet {
                        { "h|host=", host => instance.host = host },
                        { "2fa", v => instance.in2fa = v != null }
                    },
                    Run = args => instance.Run(args)
                }
            };
            return instance;
        }

        public void Run(IEnumerable<string> args)
        {
            DoLogin();
        }

        private void DoLogin()
        {
            var login = Console.ReadLine();
            var token = Console.ReadLine();
            string twofa = null;
            if (in2fa)
                twofa = Console.ReadLine();
            var credStore = new CredentialStore { Login = login, Token = token, Code = twofa };
            var hostAddress = HostAddress.Create(host);
            var client = new ApiClient(credStore, hostAddress);

            LoginResult result = null;
            if (!in2fa)
            {
                result = client.Login();
                if (result.NeedTwoFA)
                {
                    Console.WriteLine("2fa");
                    Console.WriteLine(credStore.Token);
                }
                else if (result.Success)
                {
                    Console.WriteLine(credStore.Token);
                }
                else
                {
                    Console.WriteLine("failed");
                    Console.WriteLine(result.Message);
                }
            }
            else
            {
                result = client.ContinueLogin();
                if (result.NeedTwoFA)
                {
                    Console.WriteLine("2fa");
                    Console.WriteLine(credStore.Token);
                }
                else if (result.Success)
                {
                    Console.WriteLine(credStore.Token);
                }
                else
                {
                    Console.WriteLine("failed");
                    Console.WriteLine(result.Message);
                }
            }

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Logging.LogAdapter = new ConsoleLogAdapter();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var opts = new OptionSet();
            var commands = new CommandSet("");
            foreach (var cmd in LoginCommand.Initialize().Commands)
                commands.Add(cmd);

            opts.Parse(args);
            commands.Run(args);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debugger.Break();
        }
    }
}
