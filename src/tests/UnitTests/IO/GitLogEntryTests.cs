using TestUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    public class GitLogEntryTests
    {
        [Test]
        public void ShouldEqualSelf()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
            var gitLogEntry = new GitLogEntry("CommitID",
                "AuthorName", "AuthorEmail", 
                "AuthorName", "AuthorEmail", 
                "Summary",
                "Description", 
                commitTime, commitTime, 
                new List<GitStatusEntry>(), 
                "MergeA", "MergeB");

            gitLogEntry.AssertEqual(gitLogEntry);
        }

        [Test]
        public void ShouldNotEqualAnotherWhenFieldsAreDifferent()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);

            var gitLogEntry1 = new GitLogEntry("CommitID", 
                "AuthorName", "AuthorEmail",
                "AuthorName", "AuthorEmail", 
                "Summary",
                "Description", 
                commitTime, commitTime,
                new List<GitStatusEntry>(), 
                "MergeA", "MergeB");

            var gitLogEntry2 = new GitLogEntry("`CommitID",
                "`AuthorName", "`AuthorEmail", 
                "`AuthorName", "`AuthorEmail",
                "`Summary", 
                "`Description", 
                commitTime, commitTime,
                new List<GitStatusEntry>(), 
                "`MergeA", "`MergeB");

            gitLogEntry1.AssertNotEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldEqualAnotherWhenChangesIsEmpty()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);
            var gitLogEntry1 = new GitLogEntry("CommitID", 
                "AuthorName", "AuthorEmail", 
                "AuthorName", "AuthorEmail", 
                "Summary", 
                "Description", 
                commitTime, commitTime,
                new List<GitStatusEntry>(),
                "MergeA", "MergeB");

            var gitLogEntry2 = new GitLogEntry("CommitID", 
                "AuthorName", "AuthorEmail",
                "AuthorName", "AuthorEmail", 
                "Summary",
                "Description", 
                commitTime, commitTime,
                new List<GitStatusEntry>(),
                "MergeA", "MergeB");
            
            gitLogEntry1.AssertEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldEqualAnotherWhenChangesIsNotEmpty()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);

            var gitLogEntry1 = new GitLogEntry("CommitID", 
                "AuthorName", "AuthorEmail", 
                "AuthorName", "AuthorEmail",
                "Summary", "Description", 
                commitTime, commitTime, 
                new List<GitStatusEntry>(new[]
                {
                    new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath"),
                }),
                "MergeA", "MergeB");

            var gitLogEntry2 = new GitLogEntry("CommitID", 
                "AuthorName", "AuthorEmail",
                "AuthorName", "AuthorEmail", 
                "Summary", 
                "Description",
                commitTime, commitTime, 
                new List<GitStatusEntry>(new[]
                {
                    new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,GitFileStatus.None, "SomeOriginalPath")
                }),
                "MergeA", "MergeB");

            gitLogEntry1.AssertEqual(gitLogEntry2);
        }

        [Test]
        public void ShouldNotEqualAnotherWhenChangesAreDifferent()
        {
            var commitTime = new DateTimeOffset(1921, 12, 23, 1, 3, 6, 23, TimeSpan.Zero);

            var gitLogEntry1 = new GitLogEntry("CommitID",
                "AuthorName", "AuthorEmail",
                "AuthorName", "AuthorEmail",
                "Summary", 
                "Description", 
                commitTime, commitTime, 
                new List<GitStatusEntry>(new[]
                {
                    new GitStatusEntry("ASDFASDF", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added,GitFileStatus.None, "SomeOriginalPath"),
                }), 
                "MergeA", "MergeB");

            var gitLogEntry2 = new GitLogEntry("CommitID", 
                "AuthorName", "AuthorEmail",
                "AuthorName", "AuthorEmail", 
                "Summary",
                "Description", 
                commitTime, commitTime,
                new List<GitStatusEntry>(new[]
                {
                    new GitStatusEntry("SomePath", "SomeFullPath", "SomeProjectPath", GitFileStatus.Added, GitFileStatus.None, "SomeOriginalPath")
                }), 
                "MergeA", "MergeB");

            gitLogEntry1.AssertNotEqual(gitLogEntry2);
        }
    }
}
