using GitHub.Api;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class LoginTests
    {
        //[Test]
        public async Task SimpleLogin()
        {
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
