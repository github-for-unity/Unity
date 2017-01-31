using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class LogEntryOutputProcessorTests : BaseOutputProcessorTests
    {
        [Test]
        public void ShouldParse0Commits()
        {
            var output = new[]
            {
                "fatal: your current branch 'master' does not have any commits yet",
            };

            AssertProcessOutput(output, new GitLogEntry[] {});
        }

        [Test]
        public void ShouldParseSingleRenameCommit()
        {
            var output = new[]
            {
                "commit 1ec2dd47eb6b00a5ef31da9eb13de04de57cbe9f",
                "Author: Stanley Goldman <Stanley.Goldman@gmail.com>",
                "Date:   Fri Jan 27 17:19:32 2017 -0500",
                "",
                "    Moving project files where they should be kept",
                "",
                "R100	TestDocument.txt	Assets/TestDocument.txt",
                null,
            };

            var expected = new[]
            {
                new GitLogEntry
                {
                    AuthorEmail = "Stanley.Goldman@gmail.com",
                    AuthorName = "Stanley Goldman",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("Assets/TestDocument.txt",
                            TestRootPath + @"\Assets/TestDocument.txt", null,
                            GitFileStatus.Renamed, "TestDocument.txt"),
                    },
                    CommitID = "1ec2dd47eb6b00a5ef31da9eb13de04de57cbe9f",
                    Summary = "Moving project files where they should be kept",
                    Description = "Moving project files where they should be kept",
                    Time = new DateTimeOffset(2017, 1, 27, 17, 19, 32, TimeSpan.FromHours(-5))
                }
            };

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseSingleModifyCommitWithMultipleChanges()
        {
            var output = new[]
            {
                "commit ee1cd912a5728f8fe268130791fd61ab3e69d941",
                "Author: Andreia Gaita <shana@spoiledcat.net>",
                "Date:   Tue Jan 24 16:35:24 2017 +0100",
                "",
                "    Bump version to 2.2.0.6",
                "",
                "M src/GitHub.VisualStudio/GitHub.VisualStudio.csproj",
                "M src/GitHub.VisualStudio/source.extension.vsixmanifest",
                "M src/MsiInstaller/Version.wxi",
                "M src/common/SolutionInfo.cs",
                null,
            };

            var expected = new[]
            {
                new GitLogEntry
                {
                    AuthorEmail = "shana@spoiledcat.net",
                    AuthorName = "Andreia Gaita",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("src/GitHub.VisualStudio/GitHub.VisualStudio.csproj",
                            TestRootPath + @"\src/GitHub.VisualStudio/GitHub.VisualStudio.csproj", null,
                            GitFileStatus.Modified),
                        new GitStatusEntry("src/GitHub.VisualStudio/source.extension.vsixmanifest",
                            TestRootPath + @"\src/GitHub.VisualStudio/source.extension.vsixmanifest", null,
                            GitFileStatus.Modified),
                        new GitStatusEntry("src/MsiInstaller/Version.wxi",
                            TestRootPath + @"\src/MsiInstaller/Version.wxi",
                            null, GitFileStatus.Modified),
                        new GitStatusEntry("src/common/SolutionInfo.cs",
                            TestRootPath + @"\src/common/SolutionInfo.cs",
                            null, GitFileStatus.Modified),
                    },
                    CommitID = "ee1cd912a5728f8fe268130791fd61ab3e69d941",
                    Summary = "Bump version to 2.2.0.6",
                    Description = "Bump version to 2.2.0.6",
                    Time = new DateTimeOffset(2017, 1, 24, 16, 35, 24, TimeSpan.FromHours(1))
                }
            };

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseCommitWithDescriptionText()
        {
            var output = new[]
            {
                "commit 996d8f49aca6c6454f17fb63c7ab3a033e5309ff",
                "Author: Jamie Cansdale <jcansdale@github.com>",
                "Date:   Fri Jan 27 12:21:16 2017 +0000",
                "",
                "    Add Codemania presentation link to README.md",
                "",
                "    Add 'More information' section.",
                "    Add link to Andreia Gaita's presentation at Codemania 2016 about this extension. #804",
                "",
                "M README.md",
                null,
            };

            var expected = new[]
            {
                new GitLogEntry
                {
                    AuthorEmail = "jcansdale@github.com",
                    AuthorName = "Jamie Cansdale",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("README.md",
                            TestRootPath + @"\README.md", null,
                            GitFileStatus.Modified),
                    },
                    CommitID = "996d8f49aca6c6454f17fb63c7ab3a033e5309ff",
                    Summary = "Add Codemania presentation link to README.md",
                    Description = "Add Codemania presentation link to README.md" + Environment.NewLine
                                  + "Add 'More information' section." + Environment.NewLine
                                  +
                                  "Add link to Andreia Gaita's presentation at Codemania 2016 about this extension. #804",
                    Time = new DateTimeOffset(2017, 1, 27, 12, 21, 16, TimeSpan.Zero)
                }
            };

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseCommitWithDescriptionTextThatHasNewlines()
        {
            var output = new[]
            {
                "commit 996d8f49aca6c6454f17fb63c7ab3a033e5309ff",
                "Author: Jamie Cansdale <jcansdale@github.com>",
                "Date:   Fri Jan 27 12:21:16 2017 +0000",
                "",
                "    Add Codemania presentation link to README.md",
                "",
                "    Add 'More information' section.",
                "",
                "    Add link to Andreia Gaita's presentation at Codemania 2016 about this extension. #804",
                "",
                "M README.md",
                null,
            };

            var expected = new[]
            {
                new GitLogEntry
                {
                    AuthorEmail = "jcansdale@github.com",
                    AuthorName = "Jamie Cansdale",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("README.md",
                            TestRootPath + @"\README.md", null, GitFileStatus.Modified),
                    },
                    CommitID = "996d8f49aca6c6454f17fb63c7ab3a033e5309ff",
                    Summary = "Add Codemania presentation link to README.md",
                    Description = "Add Codemania presentation link to README.md" + Environment.NewLine
                                  + "Add 'More information' section." + Environment.NewLine + Environment.NewLine
                                  + "Add link to Andreia Gaita's presentation at Codemania 2016 about this extension. #804",
                    Time = new DateTimeOffset(2017, 1, 27, 12, 21, 16, TimeSpan.Zero)
                }
            };

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseCommitWithDescriptionTextThatHasMultipleNewlines()
        {
            var output = new[]
            {
                "commit 996d8f49aca6c6454f17fb63c7ab3a033e5309ff",
                "Author: Jamie Cansdale <jcansdale@github.com>",
                "Date:   Fri Jan 27 12:21:16 2017 +0000",
                "",
                "    Add Codemania presentation link to README.md",
                "",
                "    Add 'More information' section.",
                "",
                "",
                "    Add link to Andreia Gaita's presentation at Codemania 2016 about this extension. #804",
                "",
                "M README.md",
                null,
            };

            var expected = new[]
            {
                new GitLogEntry
                {
                    AuthorEmail = "jcansdale@github.com",
                    AuthorName = "Jamie Cansdale",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("README.md",
                            TestRootPath + @"\README.md", null, GitFileStatus.Modified),
                    },
                    CommitID = "996d8f49aca6c6454f17fb63c7ab3a033e5309ff",
                    Summary = "Add Codemania presentation link to README.md",
                    Description = "Add Codemania presentation link to README.md" + Environment.NewLine
                                  + "Add 'More information' section." + Environment.NewLine + Environment.NewLine +
                                  Environment.NewLine
                                  + "Add link to Andreia Gaita's presentation at Codemania 2016 about this extension. #804",
                    Time = new DateTimeOffset(2017, 1, 27, 12, 21, 16, TimeSpan.Zero)
                }
            };

            AssertProcessOutput(output, expected);
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitLogEntry[] expected)
        {
            var gitStatusEntryFactory = CreateGitStatusEntryFactory();

            var results = new List<GitLogEntry>();
            var outputProcessor = new LogEntryOutputProcessor(gitStatusEntryFactory);
            outputProcessor.OnLogEntry += logEntry => { results.Add(logEntry); };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            results.AssertEqual(expected);
        }
    }
}