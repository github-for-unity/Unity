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
    }
}
