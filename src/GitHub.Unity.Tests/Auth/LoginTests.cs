using GitHub.Api;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class LoginTests
    {
        [Test]
        public void SimpleLogin()
        {
            var program = new Program();
            var credentialManager = new WindowsCredentialManager();
            var api = new SimpleApiClientFactory(program, credentialManager);
            var hostAddress = HostAddress.GitHubDotComHostAddress;
            var client = api.Create(UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri));
            var githubclient = new Octokit.GitHubClient(program.ProductHeader,
                        new SimpleCredentialStore(hostAddress, credentialManager),
                        hostAddress.ApiUri);
            var repo = githubclient.Repository.Get("github", "VisualStudio").Result;
            Assert.NotNull(repo);
            Assert.AreEqual("VisualStudio", repo.Name);
        }
    }
}
