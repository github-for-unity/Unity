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
                new GitLogEntry("CommitID",
                    "AuthorName", "AuthorEmail", 
                    "AuthorName", "AuthorEmail", 
                    "Summary",
                    "Description", 
                    commitTime, commitTime, 
                    new List<GitStatusEntry>(), 
                    "MergeA", "MergeB")
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
                new GitLogEntry("CommitID", 
                    "AuthorName", "AuthorEmail", 
                    "AuthorName", "AuthorEmail",
                    "Summary", 
                    "Description", 
                    commitTime, commitTime,
                    new List<GitStatusEntry>(), 
                    "MergeA", "MergeB")
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
                new GitLogEntry("CommitID", 
                    "AuthorName", "AuthorEmail", 
                    "AuthorName", "AuthorEmail",
                    "Summary", 
                    "Description", 
                    commitTime, commitTime,
                    new List<GitStatusEntry>
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath"),
                    },
                    "MergeA", "MergeB")
            };

            var otherEntries = new[]
            {
                new GitLogEntry("CommitID",
                    "AuthorName", "AuthorEmail", 
                    "AuthorName", "AuthorEmail", 
                    "Summary", 
                    "Description", 
                    commitTime, commitTime,
                    new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath"),
                    }), 
                    "MergeA", "MergeB")
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
                new GitLogEntry("CommitID",
                    "AuthorName", "AuthorEmail",
                    "AuthorName", "AuthorEmail",
                    "Summary", "Description", 
                    commitTime, commitTime,
                    new List<GitStatusEntry>
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath"),
                    },
                    "MergeA", "MergeB"),
                new GitLogEntry("`CommitID",
                    "`AuthorName", "`AuthorEmail", 
                    "`AuthorName", "`AuthorEmail", 
                    "`Summary", 
                    "`Description",
                    commitTime, commitTime,
                    new List<GitStatusEntry>(),
                    "`MergeA", "`MergeB"),
            };

            var otherEntries = new[]
            {
                new GitLogEntry("CommitID", 
                    "AuthorName", "AuthorEmail",
                    "AuthorName", "AuthorEmail", 
                    "Summary", 
                    "Description", 
                    commitTime, commitTime,
                    new List<GitStatusEntry> {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath")
                    }, 
                    "MergeA", "MergeB"),
                new GitLogEntry("`CommitID",
                    "`AuthorName", "`AuthorEmail", 
                    "`AuthorName", "`AuthorEmail", 
                    "`Summary", 
                    "`Description", 
                    commitTime, commitTime,
                    new List<GitStatusEntry>(),
                    "`MergeA", "`MergeB")
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
                new GitLogEntry("CommitID",
                    "AuthorName", "AuthorEmail",
                    "AuthorName", "AuthorEmail",
                    "Summary",
                    "Description", 
                    commitTime, commitTime,
                    new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath"),
                    }),
                    "MergeA", "MergeB"),
                new GitLogEntry("`CommitID", 
                    "`AuthorName", "`AuthorEmail", 
                    "`AuthorName", "`AuthorEmail", 
                    "`Summary", 
                    "`Description", 
                    commitTime, commitTime,
                    new List<GitStatusEntry>(),
                    "`MergeA", "`MergeB"),
            };

            var otherEntries = new[]
            {
                new GitLogEntry("`CommitID", 
                    "`AuthorName", "`AuthorEmail", 
                    "`AuthorName", "`AuthorEmail", 
                    "`Summary",
                    "`Description", 
                    otherCommitTime, otherCommitTime,
                    new List<GitStatusEntry>(), 
                    "`MergeA", "`MergeB"),
                new GitLogEntry("CommitID", 
                    "AuthorName", "AuthorEmail", 
                    "AuthorName", "AuthorEmail",
                    "Summary",
                    "Description", 
                    otherCommitTime, otherCommitTime,
                    new List<GitStatusEntry>(new[]
                    {
                        new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath"),
                    }), 
                    "MergeA", "MergeB")
            };

            entries.AssertNotEqual(otherEntries);
        }
    }
}
