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
                    TimeValue = commitTime,
                    CommitTimeValue = commitTime
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
                    TimeValue = commitTime,
                    CommitTimeValue = commitTime
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
                    TimeValue = commitTime,
                    CommitTimeValue = commitTime
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
                    TimeValue = commitTime,
                    CommitTimeValue = commitTime
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
                    TimeValue = commitTime,
                    CommitTimeValue = commitTime
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
                    TimeValue = otherCommitTime,
                    CommitTimeValue = otherCommitTime
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
                    TimeValue = commitTime,
                    CommitTimeValue = commitTime
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
                    TimeValue = otherCommitTime,
                    CommitTimeValue = otherCommitTime
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
                    TimeValue = commitTime,
                    CommitTimeValue = commitTime
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
                    TimeValue = otherCommitTime,
                    CommitTimeValue = otherCommitTime
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
                    TimeValue = otherCommitTime,
                    CommitTimeValue = otherCommitTime
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
                    TimeValue = commitTime,
                    CommitTimeValue = commitTime
                }
            };

            entries.AssertNotEqual(otherEntries);
        }
    }
}