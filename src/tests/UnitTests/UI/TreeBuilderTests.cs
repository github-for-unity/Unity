using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests.UI
{

    [TestFixture]
    public class TreeBuilderTests
    {
        [Test]
        public void FirstTreeBuilderTest()
        {
            var newGitStatusEntries = new List<GitStatusEntry>();
            var gitStatusEntries = new List<GitStatusEntry>();

            var gitCommitTargets = new List<GitCommitTarget>();
            var foldedTreeEntries1 = new List<string>();

            Action<FileTreeNode> stateChangeCallback1 = node => { };
            var fileTreeNode = TreeBuilder.BuildTree4(newGitStatusEntries, gitStatusEntries, gitCommitTargets, foldedTreeEntries1, stateChangeCallback1);
        }
    }
}
