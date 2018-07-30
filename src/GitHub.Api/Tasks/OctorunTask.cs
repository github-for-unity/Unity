using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace GitHub.Unity
{
    class OctorunResultOutputProcessor : BaseOutputProcessor<OctorunResult>
    {
        private int lineCount;
        private string status;
        private List<string> output = new List<string>();

        public override void LineReceived(string line)
        {
            if (line == null)
            {
                OctorunResult octorunResult;
                if (lineCount == 0)
                {
                    octorunResult = new OctorunResult();
                }
                else
                {
                    octorunResult = new OctorunResult(status, output.ToArray());
                }
                RaiseOnEntry(octorunResult);
                return;
            }

            if (lineCount == 0)
            {
                status = line;
            }
            else if (status == "error")
            {
                if (lineCount > 1)
                {
                    output.Add(line);
                }
            }
            else
            {
                output.Add(line);
            }

            lineCount++;
        }
    }

    class OctorunTask : ProcessTask<OctorunResult>
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string userToken;

        private readonly NPath pathToNodeJs;
        private readonly NPath pathToOctorunJs;
        private readonly string arguments;

        public OctorunTask(CancellationToken token, IKeychain keychain, IEnvironment environment,
            string arguments,
            IOutputProcessor<OctorunResult> processor = null)
            : base(token, processor ?? new OctorunResultOutputProcessor())
        {
            this.clientId = ApplicationInfo.ClientId;
            this.clientSecret = ApplicationInfo.ClientSecret;
            this.pathToNodeJs = environment.NodeJsExecutablePath;
            this.pathToOctorunJs = environment.OctorunScriptPath;
            this.arguments = $"\"{pathToOctorunJs}\" {arguments}";

            var cloneUrl = environment.Repository?.CloneUrl;
            var host = String.IsNullOrEmpty(cloneUrl)
                ? UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri)
                : new UriString(cloneUrl.ToRepositoryUri()
                    .GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));

            var adapter = keychain.Load(host, true);
            if (adapter != null)
                userToken = adapter.Credential.Token;
        }

        public override void Configure(ProcessStartInfo psi)
        {
            base.Configure(psi);

            psi.WorkingDirectory = pathToOctorunJs.Parent.Parent.Parent;

            psi.EnvironmentVariables.Add("OCTOKIT_USER_AGENT", $"{ApplicationInfo.ApplicationSafeName}/{ApplicationInfo.Version}");
            psi.EnvironmentVariables.Add("OCTOKIT_CLIENT_ID", clientId);
            psi.EnvironmentVariables.Add("OCTOKIT_CLIENT_SECRET", clientSecret);

            if (userToken != null)
            {
                psi.EnvironmentVariables.Add("OCTORUN_TOKEN", userToken);
            }
        }

        public override string ProcessName => pathToNodeJs;
        public override string ProcessArguments => arguments;
    }

    class OctorunResult
    {
        public OctorunResult()
        {
            Status = "error";
            Output = new string[0];
        }

        public OctorunResult(string status, string[] output)
        {
            Status = status;
            Output = output;
        }

        public string Status { get; }
        public string[] Output { get; }
        public bool IsSuccess => Status.Equals("success", StringComparison.InvariantCultureIgnoreCase);
        public bool IsError => Status.Equals("error", StringComparison.InvariantCultureIgnoreCase);
        public bool IsTwoFactorRequired => Status.Equals("2fa", StringComparison.InvariantCultureIgnoreCase);
    }

    static class OctorunResultExtensions
    {
        private static Regex ApiErrorMessageRegex = new Regex(@"\""message\"":\""(.*?)\""", RegexOptions.Compiled);

        internal static string GetApiErrorMessage(this OctorunResult octorunResult)
        {
            if (!octorunResult.IsError || !octorunResult.Output.Any())
            {
                return null;
            }

            var match = ApiErrorMessageRegex.Match(octorunResult.Output[0]);
            return match.Success ? match.Groups[1].Value : octorunResult.Output[0];
        }
    }
}
