using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using GitHub.Logging;

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
        private readonly string user;
        private readonly string userToken;

        private readonly NPath pathToNodeJs;
        private readonly NPath pathToOctorunJs;
        private readonly string arguments;

        public OctorunTask(CancellationToken token, NPath pathToNodeJs, NPath pathToOctorunJs, string arguments,
            string clientId = null,
            string clientSecret = null,
            string user = null,
            string userToken = null,
            IOutputProcessor<OctorunResult> processor = null)
            : base(token, processor ?? new OctorunResultOutputProcessor())
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.user = user;
            this.userToken = userToken;
            this.pathToNodeJs = pathToNodeJs;
            this.pathToOctorunJs = pathToOctorunJs;
            this.arguments = $"\"{pathToOctorunJs}\" {arguments}";
        }

        public override void Configure(ProcessStartInfo psi)
        {
            base.Configure(psi);

            psi.WorkingDirectory = pathToOctorunJs.Parent.Parent.Parent;

            psi.EnvironmentVariables.Add("OCTOKIT_USER_AGENT", $"{ApplicationInfo.ApplicationSafeName}/{ApplicationInfo.Version}");

            if (clientId != null)
            {
                psi.EnvironmentVariables.Add("OCTOKIT_CLIENT_ID", clientId);
            }

            if (clientSecret != null)
            {
                psi.EnvironmentVariables.Add("OCTOKIT_CLIENT_SECRET", clientSecret);
            }

            if (user != null)
            {
                psi.EnvironmentVariables.Add("OCTORUN_USER", user);
            }

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
        public string Status { get; }
        public string[] Output { get; }

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

        public bool IsSuccess => Status.Equals("success", StringComparison.InvariantCultureIgnoreCase);
        public bool IsError => Status.Equals("error", StringComparison.InvariantCultureIgnoreCase);
        public bool IsTwoFactorRequired => Status.Equals("2fa", StringComparison.InvariantCultureIgnoreCase);
    }

    static class OctorunResultExtensions {
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