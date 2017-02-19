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

            AssertProcessOutput(output, new[]
            {
                new GitRemote
                {
                    Function = GitRemoteFunction.Both,
                    Name = "origin",
                    Host = "github.com",
                    Url = "https://github.com/github/VisualStudio.git",
                }
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

            AssertProcessOutput(output, new[]
            {
                new GitRemote
                {
                    Function = GitRemoteFunction.Fetch,
                    Name = "origin",
                    Host = "github.com",
                    Url = "https://github.com/github/VisualStudio.git",
                }
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

            AssertProcessOutput(output, new[]
            {
                new GitRemote
                {
                    Function = GitRemoteFunction.Push,
                    Name = "origin",
                    Host = "github.com",
                    Url = "https://github.com/github/VisualStudio.git",
                }
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

            AssertProcessOutput(output, new[]
            {
                new GitRemote
                {
                    Function = GitRemoteFunction.Both,
                    Name = "origin",
                    Host = "github.com",
                    Url = "github.com:StanleyGoldman/VisualStudio.git",
                    User = "git"
                },
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
                new GitRemote
                {
                    Function = GitRemoteFunction.Both,
                    Name = "origin",
                    Host = "github.com",
                    Url = "https://github.com/github/VisualStudio.git",
                },
                new GitRemote
                {
                    Function = GitRemoteFunction.Both,
                    Name = "stanleygoldman",
                    Host = "github.com",
                    Url = "github.com:StanleyGoldman/VisualStudio.git",
                    User = "git"
                },
                new GitRemote
                {
                    Function = GitRemoteFunction.Fetch,
                    Name = "fetchOnly",
                    Host = "github.com",
                    Url = "github.com:StanleyGoldman/VisualStudio2.git",
                    User = "git"
                },
            });
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitRemote[] expected)
        {
            var results = new List<GitRemote>();

            var outputProcessor = new RemoteListOutputProcessor();
            outputProcessor.OnRemote += branch =>
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