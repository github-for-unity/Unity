using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace GitHub.Unity
{
    class OctorunTask : SimpleListProcessTask
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string user;
        private readonly string userToken;

        public OctorunTask(CancellationToken token, NPath pathToNodeJs, NPath pathToOctorunJs, string arguments,
            string clientId = null,
            string clientSecret = null,
            string user = null,
            string userToken = null,
            IOutputProcessor<string, List<string>> processor = null) 
            : base(token, pathToNodeJs, $"{pathToOctorunJs} {arguments}", processor)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.user = user;
            this.userToken = userToken;
        }

        public override void Configure(ProcessStartInfo psi)
        {
            base.Configure(psi);

            psi.EnvironmentVariables.Add("OCTOKIT_USER_AGENT", ApplicationInfo.ApplicationSafeName+ AssemblyName.Version.ToString());

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
    }
}