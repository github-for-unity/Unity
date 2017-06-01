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
        public void My_Local_Config()
        {
            var gitConfig = LoadGitConfig(@"[core]
	bare = false
	filemode = false
	symlinks = false
	ignorecase = true
	logallrefupdates = true
[core]
	repositoryformatversion = 0
[remote ""origin""]
	url = https://github.com/github/UnityInternal.git
[remote ""origin""]
	fetch = +refs/heads/*:refs/remotes/origin/*
[branch ""master""]
[branch ""master""]
[submodule ""submodules/dotnet-httpclient35""]
	url = https://github.com/shana/dotnet-httpClient35
[submodule ""submodules/octokit.net""]
	url = https://github.com/editor-tools/octokit.net.git
[lfs ""https://github.com/github/UnityInternal.git/info/lfs""]
	access = basic
[branch ""shana/thinking-about-auth""]
[branch ""fixes/settings""]
[branch ""fixes/wincred-default""]
[branch ""features/unity-53""]
[branch ""fixes/create-branch-NRE""]
[branch ""enhancement/metrics""]
[submodule ""script""]
	url = https://github.com/github/UnityBuildScripts
[branch ""enhancement/application-cache""]
[branch ""enhancement/application-cache""]
[branch ""enhancement/application-cache""]
[branch ""shana/task-system-ftw""]
[branch ""fixes/integration-test-cleanup""]
[branch ""fixes/log-rotate""]
[branch ""fixes/push-pull-buttons""]
[branch ""fixes/git-installer""]
[branch ""fixes/git-lfs""]
	remote = origin
	merge = refs/heads/fixes/git-lfs
[branch ""fixes/integration-test-false-negative""]
[branch ""enhancement/metrics""]
[branch ""fixes/git-installer""]
[branch ""fixes/push-pull-buttons""]
[branch ""disable-flailing-test""]
[branch ""ui/delete-branch""]
[branch ""ui/publish-for-all-the-world-to-see""]
[branch ""ui/i-made-a-huge-mistake""]
[branch ""fixes/integration-test-cleanup""]
[branch ""fixes/integration-test-false-negative""]
[branch ""master""]
	remote = origin
	merge = refs/heads/master
[branch ""features/appveyor-yml""]
	remote = origin
	merge = refs/heads/features/appveyor-yml
[branch ""fixes/integration-test-false-negative""]
[branch ""fixes/push-pull-buttons""]
[branch ""shana/task-system-ftw""]
	remote = origin
	merge = refs/heads/shana/task-system-ftw
[branch ""stan/debug""]
	remote = origin
	merge = refs/heads/stan/debug");
        }
    }
}
