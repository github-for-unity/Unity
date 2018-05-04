using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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
        public void ShouldInitializeWhenCacheDoesNotExist()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";

            var fileSystem = SubstituteFactory.CreateFileSystem();

            NPath.FileSystem = fileSystem;

            var credentialManager = Substitute.For<ICredentialManager>();

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());
            environment.FileSystem.Returns(fileSystem);

            var keychain = new Keychain(environment, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(@"c:\UserCachePath\connections.json");
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.DidNotReceive().WriteAllText(Args.String, Args.String);
            fileSystem.DidNotReceive().WriteAllLines(Args.String, Arg.Any<string[]>());

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());

            keychain.HasKeys.Should().BeFalse();
            keychain.Hosts.Should().BeEmpty();
        }

        [Test]
        public void ShouldInitializeWhenCacheInvalid()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions {
                FilesThatExist = new List<string> { connectionsCacheFile },
                FileContents = new Dictionary<string, IList<string>> {
                    {connectionsCacheFile, new List<string> { @"invalid json" }}
                }
            });

            NPath.FileSystem = fileSystem;

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());
            environment.FileSystem.Returns(fileSystem);

            var credentialManager = Substitute.For<ICredentialManager>();

            var keychain = new Keychain(environment, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.Received(1).ReadAllText(connectionsCacheFile);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.Received(1).WriteAllText(connectionsCacheFile, "[]");

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());

            keychain.HasKeys.Should().BeFalse();
            keychain.Hosts.Should().BeEmpty();
        }

        [Test]
        public void ShouldInitializeWhenCacheExists()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            var hostUri = new UriString("https://github.com/");

            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions {
                FilesThatExist = new List<string> { connectionsCacheFile },
                FileContents = new Dictionary<string, IList<string>> {
                    {connectionsCacheFile, new List<string> { @"[{""Host"":""https://github.com/"",""Username"":""SomeUser""}]" }}
                }
            });

            NPath.FileSystem = fileSystem;

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());
            environment.FileSystem.Returns(fileSystem);

            var credentialManager = Substitute.For<ICredentialManager>();

            var keychain = new Keychain(environment, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.Received(1).ReadAllText(connectionsCacheFile);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.DidNotReceive().WriteAllText(Args.String, Args.String);
            fileSystem.DidNotReceive().WriteAllLines(Args.String, Arg.Any<string[]>());

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());

            keychain.HasKeys.Should().BeTrue();
            keychain.Hosts.Should().BeEquivalentTo(hostUri);
        }

        [Test]
        public void ShouldLoadFromConnectionManager()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            var hostUri = new UriString("https://github.com/");

            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions {
                FilesThatExist = new List<string> { connectionsCacheFile },
                FileContents = new Dictionary<string, IList<string>> {
                    {connectionsCacheFile, new List<string> { @"[{""Host"":""https://github.com/"",""Username"":""SomeUser""}]" }}
                }
            });

            NPath.FileSystem = fileSystem;

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());
            environment.FileSystem.Returns(fileSystem);

            const string username = "SomeUser";
            const string token = "SomeToken";

            var credentialManager = Substitute.For<ICredentialManager>();
            credentialManager.Load(hostUri).Returns(info =>
            {
                var credential = Substitute.For<ICredential>();
                credential.Username.Returns(username);
                credential.Token.Returns(token);
                credential.Host.Returns(hostUri);
                return TaskEx.FromResult(credential);
            });

            var keychain = new Keychain(environment, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.Received(1).ReadAllText(connectionsCacheFile);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.DidNotReceive().WriteAllText(Args.String, Args.String);
            fileSystem.DidNotReceive().WriteAllLines(Args.String, Arg.Any<string[]>());

            var uriString = keychain.Hosts.FirstOrDefault();
            var keychainAdapter = keychain.Load(uriString).Result;
            keychainAdapter.Credential.Username.Should().Be(username);
            keychainAdapter.Credential.Token.Should().Be(token);
            keychainAdapter.Credential.Host.Should().Be(hostUri);

            credentialManager.Received(1).Load(hostUri);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());
        }

        [Test]
        public void ShouldDeleteFromCacheWhenLoadReturnsNullFromConnectionManager()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            var hostUri = new UriString("https://github.com/");

            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions {
                FilesThatExist = new List<string> { connectionsCacheFile },
                FileContents = new Dictionary<string, IList<string>> {
                    {connectionsCacheFile, new List<string> { @"[{""Host"":""https://github.com/"",""Username"":""SomeUser""}]" }}
                }
            });

            NPath.FileSystem = fileSystem;

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());
            environment.FileSystem.Returns(fileSystem);

            var credentialManager = Substitute.For<ICredentialManager>();
            credentialManager.Load(hostUri).Returns(info => TaskEx.FromResult<ICredential>(null));

            var keychain = new Keychain(environment, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.Received(1).ReadAllText(connectionsCacheFile);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.DidNotReceive().WriteAllText(Args.String, Args.String);
            fileSystem.ClearReceivedCalls();

            var uriString = keychain.Hosts.FirstOrDefault();
            var keychainAdapter = keychain.Load(uriString).Result;
            keychainAdapter.Should().BeNull();

            fileSystem.DidNotReceive().FileExists(Args.String);
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.Received(1).WriteAllText(connectionsCacheFile, "[]");
            fileSystem.DidNotReceive().WriteAllLines(Args.String, Arg.Any<string[]>());

            credentialManager.Received(1).Load(hostUri);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());
        }

        [Test]
        public void ShouldConnectSetCredentialsTokenAndSave()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            const string username = "SomeUser";
            const string password = "SomePassword";
            const string token = "SomeToken";

            var hostUri = new UriString("https://github.com/");

            var fileSystem = SubstituteFactory.CreateFileSystem();

            NPath.FileSystem = fileSystem;

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());
            environment.FileSystem.Returns(fileSystem);

            var credentialManager = Substitute.For<ICredentialManager>();

            credentialManager.Delete(Args.UriString).Returns(info => TaskEx.FromResult(0));

            credentialManager.Save(Arg.Any<ICredential>()).Returns(info => TaskEx.FromResult(0));

            var keychain = new Keychain(environment, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.DidNotReceive().WriteAllText(Args.String, Args.String);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.ClearReceivedCalls();

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());

            keychain.HasKeys.Should().BeFalse();
            keychain.Hosts.Should().BeEmpty();

            var keychainAdapter = keychain.Connect(hostUri);

            keychainAdapter.Credential.Should().BeNull();

            keychain.SetCredentials(new Credential(hostUri, username, password));

            keychainAdapter.Credential.Should().NotBeNull();
            keychainAdapter.Credential.Host.Should().Be(hostUri);
            keychainAdapter.Credential.Username.Should().Be(username);
            keychainAdapter.Credential.Token.Should().Be(password);

            keychain.SetToken(hostUri, token, username);

            keychainAdapter.Credential.Should().NotBeNull();
            keychainAdapter.Credential.Host.Should().Be(hostUri);
            keychainAdapter.Credential.Username.Should().Be(username);
            keychainAdapter.Credential.Token.Should().Be(token);

            keychain.Save(hostUri).Wait();

            fileSystem.DidNotReceive().FileExists(Args.String);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.Received(1).WriteAllText(connectionsCacheFile, @"[{""Host"":""https://github.com/"",""Username"":""SomeUser""}]");

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.Received(1).Delete(hostUri);
            credentialManager.Received(1).Save(Arg.Any<ICredential>());
        }

        [Test]
        public void ShouldConnectSetCredentialsAndClear()
        {
            const string connectionsCachePath = @"c:\UserCachePath\";
            const string connectionsCacheFile = @"c:\UserCachePath\connections.json";

            const string username = "SomeUser";
            const string password = "SomePassword";

            var hostUri = new UriString("https://github.com/");

            var fileSystem = SubstituteFactory.CreateFileSystem();

            NPath.FileSystem = fileSystem;

            var environment = SubstituteFactory.CreateEnvironment();
            environment.UserCachePath.Returns(info => connectionsCachePath.ToNPath());
            environment.FileSystem.Returns(fileSystem);

            var credentialManager = Substitute.For<ICredentialManager>();

            credentialManager.Delete(Args.UriString).Returns(info => TaskEx.FromResult(0));

            credentialManager.Save(Arg.Any<ICredential>()).Returns(info => TaskEx.FromResult(0));

            var keychain = new Keychain(environment, credentialManager);
            keychain.Initialize();

            fileSystem.Received(1).FileExists(connectionsCacheFile);
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            fileSystem.DidNotReceive().WriteAllText(Args.String, Args.String);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.ClearReceivedCalls();

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());

            keychain.HasKeys.Should().BeFalse();
            keychain.Hosts.Should().BeEmpty();

            var keychainAdapter = keychain.Connect(hostUri);

            keychainAdapter.Credential.Should().BeNull();

            keychain.SetCredentials(new Credential(hostUri, username, password));

            keychainAdapter.Credential.Should().NotBeNull();
            keychainAdapter.Credential.Host.Should().Be(hostUri);
            keychainAdapter.Credential.Username.Should().Be(username);
            keychainAdapter.Credential.Token.Should().Be(password);

            keychain.Clear(hostUri, false).Wait();

            keychainAdapter.Credential.Should().BeNull();

            fileSystem.DidNotReceive().FileExists(Args.String);
            fileSystem.DidNotReceive().FileDelete(Args.String);
            fileSystem.DidNotReceive().ReadAllText(Args.String);
            fileSystem.DidNotReceive().ReadAllLines(Args.String);
            // we never saved
            fileSystem.DidNotReceive().WriteAllText(Args.String, Args.String);

            credentialManager.DidNotReceive().Load(Args.UriString);
            credentialManager.DidNotReceive().HasCredentials();
            credentialManager.DidNotReceive().Delete(Args.UriString);
            credentialManager.DidNotReceive().Save(Arg.Any<ICredential>());
        }
    }
}
