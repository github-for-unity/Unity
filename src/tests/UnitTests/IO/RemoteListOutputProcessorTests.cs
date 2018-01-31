using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    public class RemoteListOutputProcessorTests
    {
        [Test]
        public void ShouldParseSingleHttpsBothWaysRemote()
        {
            var output = new[]
            {
                "origin https://github.com/github/VisualStudio.git (fetch)",
                "origin https://github.com/github/VisualStudio.git (push)",
                null
            };

            var name = "origin";
            var host = "github.com";
            var url = "https://github.com/github/VisualStudio.git";
            var function = GitRemoteFunction.Both;
            AssertProcessOutput(output, new[]
            {
                new GitRemote(name, host, url, function)
            });
        }

        [Test]
        public void ShouldParseSingleSshBothWaysRemote()
        {
            var output = new[]
            {
                "origin git@github.com:github-for-unity/Unity.git (fetch)",
                "origin git@github.com:github-for-unity/Unity.git (push)",
                null
            };

            var name = "origin";
            var host = "github.com";
            var url = "github.com:github-for-unity/Unity.git";
            var function = GitRemoteFunction.Both;
            var user = "git";
            AssertProcessOutput(output, new[]
            {
                new GitRemote(name, host, url, function, user)
            });
        }

        [Test]
        public void ShouldParseSingleHttpsFetchOnlyRemote()
        {
            var output = new[]
            {
                "origin https://github.com/github/VisualStudio.git (fetch)",
                null
            };

            var name = "origin";
            var function = GitRemoteFunction.Fetch;
            var host = "github.com";
            var url = "https://github.com/github/VisualStudio.git";
            AssertProcessOutput(output, new[]
            {
                new GitRemote(name, host, url, function)
            });
        }

        [Test]
        public void ShouldParseSingleHttpsPushOnlyRemote()
        {
            var output = new[]
            {
                "origin https://github.com/github/VisualStudio.git (push)",
                null
            };

            var name = "origin";
            var function = GitRemoteFunction.Push;
            var host = "github.com";
            var url = "https://github.com/github/VisualStudio.git";
            AssertProcessOutput(output, new[]
            {
                new GitRemote(name, host, url, function)
            });
        }

        [Test]
        public void ShouldParseSingleSSHRemote()
        {
            var output = new[]
            {
                "origin git@github.com:StanleyGoldman/VisualStudio.git (fetch)",
                "origin git@github.com:StanleyGoldman/VisualStudio.git (push)",
                null
            };

            var function = GitRemoteFunction.Both;
            var name = "origin";
            var host = "github.com";
            var url = "github.com:StanleyGoldman/VisualStudio.git";
            var user = "git";
            AssertProcessOutput(output, new[]
            {
                new GitRemote(name, host, url, function, user)
            });
        }

        [Test]
        public void ShouldParseMultipleRemotes()
        {
            var output = new[]
            {
                "origin https://github.com/github/VisualStudio.git (fetch)",
                "origin https://github.com/github/VisualStudio.git (push)",
                "stanleygoldman git@github.com:StanleyGoldman/VisualStudio.git (fetch)",
                "stanleygoldman git@github.com:StanleyGoldman/VisualStudio.git (push)",
                "fetchOnly git@github.com:StanleyGoldman/VisualStudio2.git (fetch)",
                null
            };

            AssertProcessOutput(output, new[]
            {
                new GitRemote("origin", "github.com", "https://github.com/github/VisualStudio.git", GitRemoteFunction.Both),
                new GitRemote("stanleygoldman", "github.com", "github.com:StanleyGoldman/VisualStudio.git", GitRemoteFunction.Both, "git"),
                new GitRemote("fetchOnly", "github.com", "github.com:StanleyGoldman/VisualStudio2.git", GitRemoteFunction.Fetch,"git")
            });
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitRemote[] expected)
        {
            var results = new List<GitRemote>();

            var outputProcessor = new RemoteListOutputProcessor();
            outputProcessor.OnEntry += branch =>
            {
                results.Add(branch);
            };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            results.ShouldAllBeEquivalentTo(expected);
        }
    }
}