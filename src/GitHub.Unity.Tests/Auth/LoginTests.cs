using GitHub.Api;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class LoginTests
    {
        [Test]
        public async void SimpleLogin()
        {
            var success = false;

            var program = new Program();
            var credentialManager = new WindowsCredentialManager();
            var api = new SimpleApiClientFactory(program, credentialManager);
            var hostAddress = HostAddress.GitHubDotComHostAddress;
            var client = api.Create(UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri));
            var githubclient = new Octokit.GitHubClient(program.ProductHeader,
                        new SimpleCredentialStore(hostAddress, credentialManager),
                        hostAddress.ApiUri);
            var repo = await githubclient.Repository.Get("github", "VisualStudio");
            Assert.NotNull(repo);
            Assert.AreEqual("VisualStudio", repo.Name);
        }
    }
}
