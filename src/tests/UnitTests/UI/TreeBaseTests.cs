using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    public class TestTreeNode : ITreeNode
    {
        public string Path { get; set; }
        public string Label { get; set; }
        public int Level { get; set; }
        public bool IsFolder { get; set; }
        public bool IsCollapsed { get; set; }
        public bool IsHidden { get; set; }
        public bool IsActive { get; set; }
        public bool TreeIsCheckable { get; set; }
        public CheckState CheckState { get; set; }
        public TestTreeData TreeData { get; set; } = TestTreeData.Default;
    }

    public struct TestTreeData : ITreeData
    {
        public static TestTreeData Default = new TestTreeData { Path = string.Empty };

        public string Path { get; set; }
        public bool IsActive { get; set; }

        public override string ToString()
        {
            return $"{{Path: {Path} IsActive: {IsActive}}}";
        }
    }

    public interface ITestTreeListener
    {
        void OnClear();
        void SetNodeIcon(TestTreeNode node);
        void CreateTreeNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden,
            bool isCollapsed, bool isChecked, TestTreeData? treeData);
        void AddCheckedNode(TestTreeNode node);
        void RemoveCheckedNode(TestTreeNode node);
        IEnumerable<string> GetCollapsedFolders();
        IEnumerable<string> GetCheckedFiles();
        TestTreeNode SelectedNode { get; set; }
        List<TestTreeNode> Nodes { get; set; }
        string Title { get; set; }
        bool DisplayRootNode { get; set; }
        bool IsSelectable { get; set; }
        bool IsCheckable { get; set; }
        string PathSeparator { get; set; }
    }

    public class TestTree : TreeBase<TestTreeNode, TestTreeData>
    {
        private readonly bool traceLogging;

        public TestTree(bool traceLogging = false)
        {
            this.traceLogging = traceLogging;
            TestTreeListener = Substitute.For<ITestTreeListener>();
        }

        public override IEnumerable<string> GetCheckedFiles()
        {
            if (traceLogging)
            {
                Logger.Trace("GetCheckedFiles");
            }
            return TestTreeListener.GetCheckedFiles();
        }

        public void TestClear()
        {
            CreatedTreeNodes.Clear();
        }

        protected override IEnumerable<string> GetCollapsedFolders()
        {
            if (traceLogging)
            {
                Logger.Trace("GetCollapsedFolders");
            }
            return TestTreeListener.GetCollapsedFolders();
        }

        protected override void RemoveCheckedNode(TestTreeNode node)
        {
            if (traceLogging)
            {
                Logger.Trace("RemoveCheckedNode");
            }
            TestTreeListener.RemoveCheckedNode(node);
        }

        protected override void AddCheckedNode(TestTreeNode node)
        {
            if (traceLogging)
            {
                Logger.Trace("AddCheckedNode");
            }
            TestTreeListener.AddCheckedNode(node);
        }

        protected override TestTreeNode CreateTreeNode(string path, string label, int level, bool isFolder,
            bool isActive, bool isHidden, bool isCollapsed, bool isChecked, TestTreeData? treeData)
        {
            if (traceLogging)
            {
                Logger.Trace(
                    "CreateTreeNode(path: {0}, label: {1}, level: {2}, isFolder: {3}, " +
                    "isActive: {4}, isHidden: {5}, isCollapsed: {6}, isChecked: {7}, treeData: {8})", path, label,
                    level, isFolder, isActive, isHidden, isCollapsed, isChecked, treeData?.ToString() ?? "[NULL]");
            }

            TestTreeListener.CreateTreeNode(path, label, level, isFolder, isActive, isHidden, isCollapsed, isChecked,
                treeData);

            var testTreeNode = new TestTreeNode {
                Path = path,
                Label = label,
                Level = level,
                CheckState = isChecked ? CheckState.Checked : CheckState.Empty,
                IsActive = isActive,
                IsFolder = isFolder,
                IsCollapsed = isCollapsed,
                IsHidden = isHidden,
                TreeData = treeData ?? TestTreeData.Default
            };

            CreatedTreeNodes.Add(testTreeNode);

            return testTreeNode;
        }

        protected override void OnClear()
        {
            if (traceLogging)
            {
                Logger.Trace("OnClear");
            }
            TestTreeListener.OnClear();
        }

        protected override void SetNodeIcon(TestTreeNode node)
        {
            if (traceLogging)
            {
                Logger.Trace("Set NodeIcon");
            }
            TestTreeListener.SetNodeIcon(node);
        }

        public ITestTreeListener TestTreeListener { get; set; }

        public List<TestTreeNode> CreatedTreeNodes { get; } = new List<TestTreeNode>();

        public override TestTreeNode SelectedNode
        {
            get
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Get SelectedNode");
                }
                return TestTreeListener.SelectedNode;
            }
            set
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Set SelectedNode");
                }
                TestTreeListener.SelectedNode = value;
            }
        }

        protected override List<TestTreeNode> Nodes
        {
            get
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Get Nodes");
                }
                return TestTreeListener.Nodes;
            }
        }

        public override string Title
        {
            get
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Get Title");
                }
                return TestTreeListener.Title;
            }
            set
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Set Title");
                }
                TestTreeListener.Title = value;
            }
        }

        public override bool DisplayRootNode
        {
            get
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Get DisplayRootNode");
                }
                return TestTreeListener.DisplayRootNode;
            }
            set
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Set DisplayRootNode");
                }
                TestTreeListener.DisplayRootNode = value;
            }
        }

        public override bool IsSelectable
        {
            get
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Get IsSelectable");
                }
                return TestTreeListener.IsSelectable;
            }
            set
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Set IsSelectable");
                }
                TestTreeListener.IsSelectable = value;
            }
        }

        public override bool IsCheckable
        {
            get
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Get IsCheckable");
                }
                return TestTreeListener.IsCheckable;
            }
            set
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Set IsCheckable");
                }
                TestTreeListener.IsCheckable = value;
            }
        }

        public override string PathSeparator
        {
            get
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Get PathSeparator");
                }
                return TestTreeListener.PathSeparator;
            }
            set
            {
                if (traceLogging)
                {
                    Logger.Trace("Property Set PathSeparator");
                }
                TestTreeListener.PathSeparator = value;
            }
        }
    }

    [TestFixture]
    public class TreeBaseTests
    {
        [Test]
        public void ShouldPopulateEmptyTree()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(true);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            testTree.Load(Enumerable.Empty<TestTreeData>());

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(1).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(1).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true
                }
            });
        }

        [Test]
        public void ShouldPopulateTreeWithSingleEntry()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(true);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            var testTreeData = new[] {
                new TestTreeData {
                    Path = "test.txt"
                }
            };
            testTree.Load(testTreeData);

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(2).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(2).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "test.txt",
                    Label = "test.txt",
                    Level = 1,
                    TreeData = testTreeData[0]
                }
            });
        }

        [Test]
        public void ShouldPopulateTreeWithTwoEntries()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(true);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            var testTreeData = new[] {
                new TestTreeData {
                    Path = "test.txt"
                },
                new TestTreeData {
                    Path = "test2.txt"
                }
            };
            testTree.Load(testTreeData);

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(3).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(3).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "test.txt",
                    Label = "test.txt",
                    Level = 1,
                    TreeData = testTreeData[0]
                },
                new TestTreeNode {
                    Path = "test2.txt",
                    Label = "test2.txt",
                    Level = 1,
                    TreeData = testTreeData[1]
                }
            });
        }

        [Test]
        public void ShouldPopulateTreeWithSingleEntryInPath()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(true);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            var testTreeData = new[] {
                new TestTreeData {
                    Path = "Folder\\test.txt"
                }
            };
            testTree.Load(testTreeData);

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(3).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(3).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder",
                    Label = "Folder",
                    Level = 1,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder\\test.txt",
                    Label = "test.txt",
                    Level = 2,
                    TreeData = testTreeData[0]
                }
            });
        }


        [Test]
        public void ShouldPopulateTreeWithTwoEntriesInPath()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(true);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            var testTreeData = new[] {
                new TestTreeData {
                    Path = "Folder\\test.txt"
                },
                new TestTreeData {
                    Path = "Folder\\test2.txt"
                }
            };
            testTree.Load(testTreeData);

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(4).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(4).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder",
                    Label = "Folder",
                    Level = 1,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder\\test.txt",
                    Label = "test.txt",
                    Level = 2,
                    TreeData = testTreeData[0]
                },
                new TestTreeNode {
                    Path = "Folder\\test2.txt",
                    Label = "test2.txt",
                    Level = 2,
                    TreeData = testTreeData[1]
                }
            });
        }

        [Test]
        public void ShouldPopulateTreeWithSingleEntryInDeepPath()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(true);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            var testTreeData = new[] {
                new TestTreeData {
                    Path = "Folder\\SubFolder\\test.txt"
                }
            };
            testTree.Load(testTreeData);

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(4).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(4).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder",
                    Label = "Folder",
                    Level = 1,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder\\SubFolder",
                    Label = "SubFolder",
                    Level = 2,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder\\SubFolder\\test.txt",
                    Label = "test.txt",
                    Level = 3,
                    TreeData = testTreeData[0]
                }
            });
        }

        [Test]
        public void ShouldPopulateTreeWithTwoEntriesInDeepPath()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(true);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            var testTreeData = new[] {
                new TestTreeData {
                    Path = "Folder\\SubFolder\\test.txt"
                },
                new TestTreeData {
                    Path = "Folder\\SubFolder\\test2.txt"
                }
            };
            testTree.Load(testTreeData);

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(5).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(5).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder",
                    Label = "Folder",
                    Level = 1,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder\\SubFolder",
                    Label = "SubFolder",
                    Level = 2,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "Folder\\SubFolder\\test.txt",
                    Label = "test.txt",
                    Level = 3,
                    TreeData = testTreeData[0]
                },
                new TestTreeNode {
                    Path = "Folder\\SubFolder\\test2.txt",
                    Label = "test2.txt",
                    Level = 3,
                    TreeData = testTreeData[1]
                }
            });
        }

        [Test]
        public void ShouldPopulateTreeWithMixOfEntriesHidingRootNode()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(false);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            var testTreeData = new[] {
                new TestTreeData {
                    Path = "AFile.txt"
                },
                new TestTreeData {
                    Path = "BFolder\\test.txt"
                },
                new TestTreeData {
                    Path = "BFolder\\SubFolder\\test.txt"
                },
                new TestTreeData {
                    Path = "CFolder\\test.txt"
                },
                new TestTreeData {
                    Path = "DFile.txt"
                },
            };
            testTree.Load(testTreeData);

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(9).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(9).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true,
                    Level = -1
                },
                new TestTreeNode {
                    Path = "AFile.txt",
                    Label = "AFile.txt",
                    Level = 0,
                    TreeData = testTreeData[0]
                },
                new TestTreeNode {
                    Path = "BFolder",
                    Label = "BFolder",
                    Level = 0,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "BFolder\\test.txt",
                    Label = "test.txt",
                    Level = 1,
                    TreeData = testTreeData[1]
                },
                new TestTreeNode {
                    Path = "BFolder\\SubFolder",
                    Label = "SubFolder",
                    Level = 1,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "BFolder\\SubFolder\\test.txt",
                    Label = "test.txt",
                    Level = 2,
                    TreeData = testTreeData[2]
                },
                new TestTreeNode {
                    Path = "CFolder",
                    Label = "CFolder",
                    Level = 0,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "CFolder\\test.txt",
                    Label = "test.txt",
                    Level = 1,
                    TreeData = testTreeData[3]
                },
                new TestTreeNode {
                    Path = "DFile.txt",
                    Label = "DFile.txt",
                    Level = 0,
                    TreeData = testTreeData[4]
                }
            });
        }

        [Test]
        public void ShouldPopulateTreeWithMixOfEntries()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode)null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());
            testTreeListener.PathSeparator.Returns(@"\");
            testTreeListener.DisplayRootNode.Returns(true);
            testTreeListener.IsSelectable.Returns(false);
            testTreeListener.Title.Returns("Test Tree");

            var testTreeData = new[] {
                new TestTreeData {
                    Path = "AFile.txt"
                },
                new TestTreeData {
                    Path = "BFolder\\test.txt"
                },
                new TestTreeData {
                    Path = "BFolder\\SubFolder\\test.txt"
                },
                new TestTreeData {
                    Path = "CFolder\\test.txt"
                },
                new TestTreeData {
                    Path = "DFile.txt"
                },
            };
            testTree.Load(testTreeData);

            testTreeListener.Received(1).OnClear();
            testTreeListener.Received(1).SelectedNode = null;

            testTreeListener.Received(9).CreateTreeNode(Args.String, Args.String, Args.Int, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Args.Bool, Arg.Any<TestTreeData?>());
            testTreeListener.Received(9).SetNodeIcon(Arg.Any<TestTreeNode>());

            testTree.CreatedTreeNodes.ShouldAllBeEquivalentTo(new[] {
                new TestTreeNode {
                    Path = "Test Tree",
                    Label = "Test Tree",
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "AFile.txt",
                    Label = "AFile.txt",
                    Level = 1,
                    TreeData = testTreeData[0]
                },
                new TestTreeNode {
                    Path = "BFolder",
                    Label = "BFolder",
                    Level = 1,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "BFolder\\test.txt",
                    Label = "test.txt",
                    Level = 2,
                    TreeData = testTreeData[1]
                },
                new TestTreeNode {
                    Path = "BFolder\\SubFolder",
                    Label = "SubFolder",
                    Level = 2,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "BFolder\\SubFolder\\test.txt",
                    Label = "test.txt",
                    Level = 3,
                    TreeData = testTreeData[2]
                },
                new TestTreeNode {
                    Path = "CFolder",
                    Label = "CFolder",
                    Level = 1,
                    IsFolder = true
                },
                new TestTreeNode {
                    Path = "CFolder\\test.txt",
                    Label = "test.txt",
                    Level = 2,
                    TreeData = testTreeData[3]
                },
                new TestTreeNode {
                    Path = "DFile.txt",
                    Label = "DFile.txt",
                    Level = 1,
                    TreeData = testTreeData[4]
                }
            });
        }
    }
}
