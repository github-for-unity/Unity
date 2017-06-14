using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GitHub.Unity;
using NCrunch.Framework;
using NSubstitute;
using NUnit.Framework;
using TestUtils;
using System.Threading.Tasks;
using Octokit;

namespace UnitTests
{
    [TestFixture, Isolated]
    public class KeychainTests
    {
        private static readonly SubstituteFactory SubstituteFactory = new SubstituteFactory();

        [Test]
        public void Should_Initialize_When_Cache_Does_Not_Exist()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());

            var fileSystem = SubstituteFactory.CreateFileSystem();
            var credentialManager = NSubstitute.Substitute.For<ICredentialManager>();

            var keychain = new Keychain(environment, fileSystem, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(@"c:\UserCachePath\connections.json");
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.DidNotReceive().FileDelete(Args.String);

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());

            keychain.HasKeys.Should().BeFalse();
            keychain.Connections.Should().BeEmpty();
        }

        [Test]
        public void Should_Initialize_When_Cache_Invalid()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());

            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions()
            {
                FilesThatExist = new List<string> { connectionsCacheFile },
                FileContents = new Dictionary<string, IList<string>> {
                    {connectionsCacheFile, new List<string> { @"invalid json" }}
                }
            });

            var credentialManager = Substitute.For<ICredentialManager>();

            var keychain = new Keychain(environment, fileSystem, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.Received(1).ReadAllText(connectionsCacheFile);
            fileSystem.Received(1).FileDelete(connectionsCacheFile);

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());

            keychain.HasKeys.Should().BeFalse();
            keychain.Connections.Should().BeEmpty();
        }

        [Test]
        public void Should_Initialize_When_Cache_Exists()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            var gitHubDotComUriString = new UriString("https://github.com/");

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());

            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions()
            {
                FilesThatExist = new List<string> { connectionsCacheFile },
                FileContents = new Dictionary<string, IList<string>> {
                    {connectionsCacheFile, new List<string> { @"[{""Host"":""https://github.com/"",""Username"":""StanleyGoldman""}]" }}
                }
            });

            var credentialManager = Substitute.For<ICredentialManager>();

            var keychain = new Keychain(environment, fileSystem, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.Received(1).ReadAllText(connectionsCacheFile);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.DidNotReceive().WriteAllText(Args.String, Args.String);
            fileSystem.DidNotReceive().WriteAllLines(Args.String, Arg.Any<string[]>());

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());

            keychain.HasKeys.Should().BeTrue();
            keychain.Connections.Should().BeEquivalentTo(gitHubDotComUriString);
        }

        [Test]
        public void Should_Load_From_ConnectionManager()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            var gitHubDotComUriString = new UriString("https://github.com/");

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());

            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions()
            {
                FilesThatExist = new List<string> { connectionsCacheFile },
                FileContents = new Dictionary<string, IList<string>> {
                    {connectionsCacheFile, new List<string> { @"[{""Host"":""https://github.com/"",""Username"":""StanleyGoldman""}]" }}
                }
            });

            const string username = "SomeUser";
            const string token = "SomeToken";

            var credentialManager = Substitute.For<ICredentialManager>();
            credentialManager.Load(gitHubDotComUriString).Returns(info => {
                var credential = Substitute.For<ICredential>();
                credential.Username.Returns(username);
                credential.Token.Returns(token);
                credential.Host.Returns(gitHubDotComUriString);
                return TaskEx.FromResult(credential);
            });

            var keychain = new Keychain(environment, fileSystem, credentialManager);
            keychain.Initialize();

            var uriString = keychain.Connections.FirstOrDefault();
            var keychainAdapter = keychain.Load(uriString).Result;
            keychainAdapter.Credential.Username.Should().Be(username);
            keychainAdapter.Credential.Token.Should().Be(token);
            keychainAdapter.Credential.Host.Should().Be(gitHubDotComUriString);

            keychainAdapter.OctokitCredentials.AuthenticationType.Should().Be(AuthenticationType.Basic);
            keychainAdapter.OctokitCredentials.Login.Should().Be(username);
            keychainAdapter.OctokitCredentials.Password.Should().Be(token);
        }

        [Test]
        public void Should_Delete_From_Cache_When_Load_Returns_Null_From_ConnectionManager()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            var gitHubDotComUriString = new UriString("https://github.com/");

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());

            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions()
            {
                FilesThatExist = new List<string> { connectionsCacheFile },
                FileContents = new Dictionary<string, IList<string>> {
                    {connectionsCacheFile, new List<string> { @"[{""Host"":""https://github.com/"",""Username"":""StanleyGoldman""}]" }}
                }
            });

            var credentialManager = Substitute.For<ICredentialManager>();
            credentialManager.Load(gitHubDotComUriString).Returns(info => TaskEx.FromResult<ICredential>(null));

            var keychain = new Keychain(environment, fileSystem, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.Received(1).ReadAllText(connectionsCacheFile);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.ClearReceivedCalls();

            var uriString = keychain.Connections.FirstOrDefault();
            var keychainAdapter = keychain.Load(uriString).Result;
            keychainAdapter.Credential.Should().BeNull();

            keychainAdapter.OctokitCredentials.AuthenticationType.Should().Be(AuthenticationType.Anonymous);
            keychainAdapter.OctokitCredentials.Login.Should().BeNull();
            keychainAdapter.OctokitCredentials.Password.Should().BeNull();

            fileSystem.DidNotReceive().FileExists(Args.String);
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.Received(1).WriteAllText(connectionsCacheFile, "[]");
            fileSystem.DidNotReceive().WriteAllLines(Args.String, Arg.Any<string[]>());
        }
    }
}
