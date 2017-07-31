using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using TestUtils;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    public class GitLogEntryListTests
    {
        [Test]
        public void NullListShouldEqualNullList()
        {
            GitLogEntry[] entries = null;
            GitLogEntry[] otherEntries = null;

            entries.AssertEqual(otherEntries);
        }

        [Test]
        public void NullListShouldNotEqualEmptyList()
        {
            GitLogEntry[] entries = {};
            GitLogEntry[] otherEntries = null;

            entries.AssertNotEqual(otherEntries);
        }

        [Test]
        public void NullListShouldNotEqualListOf1()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
            var entries = new[]
            {
                new GitLogEntry
                {
                    AuthorName = "AuthorName",
                    AuthorEmail = "AuthorEmail",
                    MergeA = "MergeA",
                    MergeB = "MergeB",
                    Changes = new List<GitStatusEntry>(),
                    CommitID = "CommitID",
                    Summary = "Summary",
                    Description = "Description",
                    TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                }
            };
            GitLogEntry[] otherEntries = null;

            entries.AssertNotEqual(otherEntries);
        }

        [Test]
        public void EmptyListShouldNotEqualListOf1()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
            var entries = new[]
            {
                new GitLogEntry
                {
                    AuthorName = "AuthorName",
                    AuthorEmail = "AuthorEmail",
                    MergeA = "MergeA",
                    MergeB = "MergeB",
                    Changes = new List<GitStatusEntry>(),
                    CommitID = "CommitID",
                    Summary = "Summary",
                    Description = "Description",
                    TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                }
            };
            GitLogEntry[] otherEntries = new GitLogEntry[0];

            entries.AssertNotEqual(otherEntries);
        }

        [Test]
        public void ListOf1ShouldEqualListOf1()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
            var entries = new[]
            {
                new GitLogEntry
                {
                    AuthorName = "AuthorName",
                    AuthorEmail = "AuthorEmail",
                    MergeA = "MergeA",
                    MergeB = "MergeB",
                    Changes = new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                            "SomeOriginalPath"),
                    }),
                    CommitID = "CommitID",
                    Summary = "Summary",
                    Description = "Description",
                    TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                }
            };

            var otherEntries = new[]
            {
                new GitLogEntry
                {
                    AuthorName = "AuthorName",
                    AuthorEmail = "AuthorEmail",
                    MergeA = "MergeA",
                    MergeB = "MergeB",
                    Changes = new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                            "SomeOriginalPath"),
                    }),
                    CommitID = "CommitID",
                    Summary = "Summary",
                    Description = "Description",
                    TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                }
            };

            entries.AssertEqual(otherEntries);
        }

        [Test]
        public void ListOf2ShouldEqualListOf2()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
            var otherCommitTime = new DateTimeOffset(1981, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);

            var entries = new[]
            {
                new GitLogEntry
                {
                    AuthorName = "AuthorName",
                    AuthorEmail = "AuthorEmail",
                    MergeA = "MergeA",
                    MergeB = "MergeB",
                    Changes = new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                            "SomeOriginalPath"),
                    }),
                    CommitID = "CommitID",
                    Summary = "Summary",
                    Description = "Description",
                    TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                },
                new GitLogEntry
                {
                    AuthorName = "OtherAuthorName",
                    AuthorEmail = "OtherAuthorEmail",
                    MergeA = "OtherMergeA",
                    MergeB = "OtherMergeB",
                    Changes = new List<GitStatusEntry>(),
                    CommitID = "OtherCommitID",
                    Summary = "OtherSummary",
                    Description = "OtherDescription",
                    TimeString = otherCommitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = otherCommitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                }
            };

            var otherEntries = new[]
            {
                new GitLogEntry
                {
                    AuthorName = "AuthorName",
                    AuthorEmail = "AuthorEmail",
                    MergeA = "MergeA",
                    MergeB = "MergeB",
                    Changes = new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                            "SomeOriginalPath"),
                    }),
                    CommitID = "CommitID",
                    Summary = "Summary",
                    Description = "Description",
                    TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                },
                new GitLogEntry
                {
                    AuthorName = "OtherAuthorName",
                    AuthorEmail = "OtherAuthorEmail",
                    MergeA = "OtherMergeA",
                    MergeB = "OtherMergeB",
                    Changes = new List<GitStatusEntry>(),
                    CommitID = "OtherCommitID",
                    Summary = "OtherSummary",
                    Description = "OtherDescription",
                    TimeString = otherCommitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = otherCommitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                }
            };

            entries.AssertEqual(otherEntries);
        }

        [Test]
        public void ListOf2ShouldNotEqualListOf2InDifferentOrder()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
            var otherCommitTime = new DateTimeOffset(1981, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);

            var entries = new[]
            {
                new GitLogEntry
                {
                    AuthorName = "AuthorName",
                    AuthorEmail = "AuthorEmail",
                    MergeA = "MergeA",
                    MergeB = "MergeB",
                    Changes = new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                            "SomeOriginalPath"),
                    }),
                    CommitID = "CommitID",
                    Summary = "Summary",
                    Description = "Description",
                    TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                },
                new GitLogEntry
                {
                    AuthorName = "OtherAuthorName",
                    AuthorEmail = "OtherAuthorEmail",
                    MergeA = "OtherMergeA",
                    MergeB = "OtherMergeB",
                    Changes = new List<GitStatusEntry>(),
                    CommitID = "OtherCommitID",
                    Summary = "OtherSummary",
                    Description = "OtherDescription",
                    TimeString = otherCommitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = otherCommitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                }
            };

            var otherEntries = new[]
            {
                new GitLogEntry
                {
                    AuthorName = "OtherAuthorName",
                    AuthorEmail = "OtherAuthorEmail",
                    MergeA = "OtherMergeA",
                    MergeB = "OtherMergeB",
                    Changes = new List<GitStatusEntry>(),
                    CommitID = "OtherCommitID",
                    Summary = "OtherSummary",
                    Description = "OtherDescription",
                    TimeString = otherCommitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = otherCommitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                },
                new GitLogEntry
                {
                    AuthorName = "AuthorName",
                    AuthorEmail = "AuthorEmail",
                    MergeA = "MergeA",
                    MergeB = "MergeB",
                    Changes = new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,
                            "SomeOriginalPath"),
                    }),
                    CommitID = "CommitID",
                    Summary = "Summary",
                    Description = "Description",
                    TimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                    CommitTimeString = commitTime.ToString(DateTimeFormatInfo.CurrentInfo),
                }
            };

            entries.AssertNotEqual(otherEntries);
        }
    }
}