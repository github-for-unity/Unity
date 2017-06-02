using System;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests.Git
{
    public class GitConfigTests
    {
        private static GitConfig LoadGitConfig(string s)
        {
            var input = s.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            var gitConfigFileManager = Substitute.For<IGitConfigFileManager>();
            gitConfigFileManager.Lines.Returns(input);

            return new GitConfig(gitConfigFileManager);
        }

        [Test]
        public void Unclean_Config()
        {
            var gitConfig = LoadGitConfig(@"[core]
	blah = 1234
[branch ""troublesome-branch""]
[branch ""unsuspecting-branch""]
	remote = origin
	merge = refs/heads/unsuspecting-branch
[branch ""troublesome-branch""]
	remote = origin
	merge = refs/heads/troublesome-branch");
        }

        [Test]
        public void Can_Get_Values()
        {
            var gitConfig = LoadGitConfig(@"[core]
	intValue = 1234
	boolValue = true
	stringValue = refs/heads/unsuspecting-branch");

            gitConfig.GetInt("core", "intValue").Should().Be(1234);
            gitConfig.GetString("core", "boolValue").Should().Be("true");
            gitConfig.GetString("core", "stringValue").Should().Be("refs/heads/unsuspecting-branch");
        }

        [Test]
        public void Can_TryGet_Values()
        {
            var gitConfig = LoadGitConfig(@"[core]
	intValue = 1234
	boolValue = true
	stringValue = refs/heads/unsuspecting-branch");

            int intResult;
            gitConfig.TryGet("core", "intValue", out intResult).Should().BeTrue();
            intResult.Should().Be(1234);

            string boolResult;
            gitConfig.TryGet("core", "boolValue", out boolResult).Should().BeTrue();
            boolResult.Should().Be("true");

            string stringResult;
            gitConfig.TryGet("core", "stringValue", out stringResult).Should().BeTrue();
            stringResult.Should().Be("refs/heads/unsuspecting-branch");
        }
    }
}
