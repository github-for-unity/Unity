using System;
using System.Collections.Generic;
using FluentAssertions;
using GitHub.Unity;
using NCrunch.Framework;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture, Isolated]
    public class GitConfigTests
    {
        private static readonly SubstituteFactory SubstituteFactory = new SubstituteFactory();

        private static GitConfig LoadGitConfig(string configFileContents)
        {
            var input = configFileContents.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            const string configFilePath = @"c:\gitconfig.txt";

            var fileSystem = SubstituteFactory.CreateFileSystem(
                new CreateFileSystemOptions
                {
                    FileContents = new Dictionary<string, IList<string>> { { configFilePath, input } }
                });

            NPathFileSystemProvider.Current = fileSystem;

            return new GitConfig(configFilePath);
        }

        private const string NormalConfig = @"[core]
	intValue = 1234
	floatValue = 1234.5
	stringValue = refs/heads/string-value
[branch ""working-branch-1""]
	intValue = 5678
	floatValue = 5678.9
	stringValue = refs/heads/working-branch-1
[branch ""working-branch-2""]
	intValue = 3456
	floatValue = 3456.7
	stringValue = refs/heads/working-branch-2";

        private const string MalformedConfig = @"[branch ""troublesome-branch""]
    someValue = refs/heads/test-parse
[branch ""unsuspecting-branch""]
	intValue = 1234
	floatValue = 1234.5
	stringValue = refs/heads/unsuspecting-branch-value
[branch ""troublesome-branch""]
	intValue = 5678
	floatValue = 5678.9
	stringValue = refs/heads/troublesome-branch-value";

        [TestCase(NormalConfig, "core", 1234, TestName = "Can Get Root Section Int Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-1""", 5678, TestName = "Can Get Group Section Int Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-2""", 3456, TestName = "Can Get Other Group Section Int Value")]
        [TestCase(MalformedConfig, @"branch ""unsuspecting-branch""", 1234, TestName = "Can Get Group Section Int Value From Malformed")]
        [TestCase(MalformedConfig, @"branch ""troublesome-branch""", 5678, TestName = "Can Get Other Group Section Int Value From Malformed")]
        public void Can_Get_Int(string config, string section, int expected)
        {
            var gitConfig = LoadGitConfig(config);
            gitConfig.GetInt(section, "intValue").Should().Be(expected);
        }

        [TestCase(NormalConfig, "core", 1234.5f, TestName = "Can Get Root Section Float Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-1""", 5678.9f, TestName = "Can Get Group Section Float Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-2""", 3456.7f, TestName = "Can Get Other Group Section Float Value")]
        [TestCase(MalformedConfig, @"branch ""unsuspecting-branch""", 1234.5f, TestName = "Can Get Group Section Float Value From Malformed")]
        [TestCase(MalformedConfig, @"branch ""troublesome-branch""", 5678.9f, TestName = "Can Get Other Group Section Float Value From Malformed")]
        public void Can_Get_Float(string config, string section, float expected)
        {
            var gitConfig = LoadGitConfig(config);
            gitConfig.GetFloat(section, "floatValue").Should().Be(expected);
        }

        [TestCase(NormalConfig, "core", "refs/heads/string-value", TestName = "Can Get Root Section String Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-1""", "refs/heads/working-branch-1", TestName = "Can Get Group Section String Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-2""", "refs/heads/working-branch-2", TestName = "Can Get Other Group Section String Value")]
        [TestCase(MalformedConfig, @"branch ""unsuspecting-branch""", "refs/heads/unsuspecting-branch-value", TestName = "Can Get Group Section String Value From Malformed")]
        [TestCase(MalformedConfig, @"branch ""troublesome-branch""", "refs/heads/troublesome-branch-value", TestName = "Can Get Other Group Section String Value From Malformed")]
        public void Can_Get_String(string config, string section, string expected)
        {
            var gitConfig = LoadGitConfig(config);
            gitConfig.GetString(section, "stringValue").Should().Be(expected);
        }

        [TestCase(MalformedConfig, @"branch ""troublesome-branch""", "refs/heads/test-parse", TestName = "Can Get Other Group Section Other String Value From Malformed")]
        public void Can_Get_Other_String(string config, string section, string expected)
        {
            var gitConfig = LoadGitConfig(config);
            gitConfig.GetString(section, "someValue").Should().Be(expected);
        }

        [TestCase(NormalConfig, "core", 1234, TestName = "Can TryGet Root Section Int Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-1""", 5678, TestName = "Can TryGet Group Section Int Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-2""", 3456, TestName = "Can TryGet Other Group Section Int Value")]
        [TestCase(MalformedConfig, @"branch ""unsuspecting-branch""", 1234, TestName = "Can TryGet Group Section Int Value From Malformed")]
        [TestCase(MalformedConfig, @"branch ""troublesome-branch""", 5678, TestName = "Can TryGet Other Group Section Int Value From Malformed")]
        public void Can_TryGet_Int(string config, string section, int expected)
        {
            var gitConfig = LoadGitConfig(config);
            int value;
            gitConfig.TryGet(section, "intValue", out value).Should().BeTrue();
            value.Should().Be(expected);
        }

        [TestCase(NormalConfig, "core", 1234.5f, TestName = "Can TryGet Root Section Float Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-1""", 5678.9f, TestName = "Can TryGet Group Section Float Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-2""", 3456.7f, TestName = "Can TryGet Other Group Section Float Value")]
        [TestCase(MalformedConfig, @"branch ""unsuspecting-branch""", 1234.5f, TestName = "Can TryGet Group Section Float Value From Malformed")]
        [TestCase(MalformedConfig, @"branch ""troublesome-branch""", 5678.9f, TestName = "Can TryGet Other Group Section Float Value From Malformed")]
        public void Can_TryGet_Float(string config, string section, float expected)
        {
            var gitConfig = LoadGitConfig(config);
            float value;
            gitConfig.TryGet(section, "floatValue", out value).Should().BeTrue();
            value.Should().Be(expected);
        }

        [TestCase(NormalConfig, "core", "refs/heads/string-value", TestName = "Can TryGet Root Section String Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-1""", "refs/heads/working-branch-1", TestName = "Can TryGet Group Section String Value")]
        [TestCase(NormalConfig, @"branch ""working-branch-2""", "refs/heads/working-branch-2", TestName = "Can TryGet Other Group Section String Value")]
        [TestCase(MalformedConfig, @"branch ""unsuspecting-branch""", "refs/heads/unsuspecting-branch-value", TestName = "Can TryGet Group Section String Value From Malformed")]
        [TestCase(MalformedConfig, @"branch ""troublesome-branch""", "refs/heads/troublesome-branch-value", TestName = "Can TryGet Other Group Section String Value From Malformed")]
        public void Can_TryGet_String(string config, string section, string expected)
        {
            var gitConfig = LoadGitConfig(config);
            string value;
            gitConfig.TryGet(section, "stringValue", out value).Should().BeTrue();
            value.Should().Be(expected);
        }
    }
}
