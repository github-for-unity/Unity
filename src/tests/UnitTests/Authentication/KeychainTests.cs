using System.Collections.Generic;
using GitHub.Unity;
using NCrunch.Framework;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture, Isolated]
    public class KeychainTests
    {
        private static readonly SubstituteFactory SubstituteFactory = new SubstituteFactory();

        [Test]
        public void Should_Initialize_When_Cache_Does_Not_Exist()
        {
            const string connectionsCachePath = "c:\\UserCachePath\\";

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());

            var fileSystem = SubstituteFactory.CreateFileSystem();
            var credentialManager = NSubstitute.Substitute.For<ICredentialManager>();

            var keychain = new Keychain(environment, fileSystem, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(@"c:\UserCachePath\connections.json");
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
        }

        [Test]
        public void Should_Initialize_When_Cache_Invalid()
        {
            const string connectionsCachePath = "c:\\UserCachePath\\";
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
        }

        [Test]
        public void Should_Initialize_When_Cache_Exists()
        {
            const string connectionsCachePath = "c:\\UserCachePath\\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

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
        }
    }
}
