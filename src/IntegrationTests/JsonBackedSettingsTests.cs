using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests
{
    class JsonBackedSettingsTests: BaseGitEnvironmentTest
    {
        [Test, Ignore("For some reason this fails")]
        public void UserSettingsTest()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized, true);

            var userSettings = new UserSettings(Environment, "GitHubTest");
            userSettings.Initialize();

            var connectionCacheItems = new[] {
                new ConnectionCacheItem { Host = "Host1", Username = "User1" },
                new ConnectionCacheItem { Host = "Host2", Username = "User2" }
            };

            connectionCacheItems.ShouldAllBeEquivalentTo(new[] {
                new ConnectionCacheItem { Host = "Host1", Username = "User1" },
                new ConnectionCacheItem { Host = "Host2", Username = "User2" }
            });

            userSettings.Set("connectionCacheItems", connectionCacheItems);

            var json = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData)
                                         .ToNPath()
                                         .Combine("GitHubTest", "settings.json")
                                         .ReadAllText();

            json.Should()
                       .Be(@"{""GitHubUnity"":{""connectionCacheItems"":[{""Host"":""Host1"",""Username"":""User1""},{""Host"":""Host2"",""Username"":""User2""}]}}");

            userSettings = new UserSettings(Environment, "GitHubTest");
            userSettings.Initialize();

            var loadedItems = userSettings.Get<ConnectionCacheItem[]>("connectionCacheItems");

            loadedItems.Should().NotBeNull();
            connectionCacheItems.ShouldAllBeEquivalentTo(loadedItems);
        }
    }
}