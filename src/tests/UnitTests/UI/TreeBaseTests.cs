using System.Collections.Generic;
using System.Linq;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;

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
    }

    public struct TestTreeData: ITreeData
    {
        public string Path { get; }
        public bool IsActive { get; }
    }

    public interface ITestTreeListener
    {
        void OnClear();
        void SetNodeIcon(TestTreeNode node);
        TestTreeNode CreateTreeNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden, bool isCollapsed, bool isChecked, TestTreeData? treeData);
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
        public static ILogging Logger = Logging.GetLogger<TestTree>();
        private readonly bool traceLogging;

        public TestTree(bool traceLogging = false)
        {
            this.traceLogging = traceLogging;
            TestTreeListener = Substitute.For<ITestTreeListener>();
        }

        public ITestTreeListener TestTreeListener { get; set; }

        public override IEnumerable<string> GetCheckedFiles()
        {
            if (traceLogging) Logger.Trace("GetCheckedFiles");
            return TestTreeListener.GetCheckedFiles();
        }

        protected override IEnumerable<string> GetCollapsedFolders()
        {
            if (traceLogging) Logger.Trace("GetCollapsedFolders");
            return TestTreeListener.GetCollapsedFolders();
        }

        protected override void RemoveCheckedNode(TestTreeNode node)
        {
            if (traceLogging) Logger.Trace("RemoveCheckedNode");
            TestTreeListener.RemoveCheckedNode(node);
        }

        protected override void AddCheckedNode(TestTreeNode node)
        {
            if (traceLogging) Logger.Trace("AddCheckedNode");
            TestTreeListener.AddCheckedNode(node);
        }

        protected override TestTreeNode CreateTreeNode(string path, string label, int level, bool isFolder, bool isActive, bool isHidden,
            bool isCollapsed, bool isChecked, TestTreeData? treeData)
        {
            if (traceLogging) Logger.Trace("CreateTreeNode");
            return TestTreeListener.CreateTreeNode(path, label, level, isFolder, isActive, isHidden, isCollapsed, isChecked, treeData);
        }

        protected override void OnClear()
        {
            if (traceLogging) Logger.Trace("OnClear");
            TestTreeListener.OnClear();
        }

        protected override void SetNodeIcon(TestTreeNode node)
        {
            if (traceLogging) Logger.Trace("Set NodeIcon");
            TestTreeListener.SetNodeIcon(node);
        }

        public override TestTreeNode SelectedNode
        {
            get
            {
                if (traceLogging) Logger.Trace("Get SelectedNode");
                return TestTreeListener.SelectedNode;
            }
            set
            {
                if (traceLogging) Logger.Trace("Set SelectedNode");
                TestTreeListener.SelectedNode = value;
            }
        }

        protected override List<TestTreeNode> Nodes
        {
            get
            {
                if (traceLogging) Logger.Trace("Get Nodes");
                return TestTreeListener.Nodes;
            }
        }

        public override string Title
        {
            get
            {
                if (traceLogging) Logger.Trace("Get Title");
                return TestTreeListener.Title;
            }
            set
            {
                if (traceLogging) Logger.Trace("Set Title");
                TestTreeListener.Title = value;
            }
        }

        public override bool DisplayRootNode
        {
            get
            {
                if (traceLogging) Logger.Trace("Get DisplayRootNode");
                return TestTreeListener.DisplayRootNode;
            }
            set
            {
                if (traceLogging) Logger.Trace("Set DisplayRootNode");
                TestTreeListener.DisplayRootNode = value;
            }
        }

        public override bool IsSelectable
        {
            get
            {
                if (traceLogging) Logger.Trace("Get IsSelectable");
                return TestTreeListener.IsSelectable;
            }
            set
            {
                if (traceLogging) Logger.Trace("Set IsSelectable");
                TestTreeListener.IsSelectable = value;
            }
        }

        public override bool IsCheckable
        {
            get
            {
                if (traceLogging) Logger.Trace("Get IsCheckable");
                return TestTreeListener.IsCheckable;
            }
            set
            {
                if (traceLogging) Logger.Trace("Set IsCheckable");
                TestTreeListener.IsCheckable = value;
            }
        }

        public override string PathSeparator
        {
            get
            {
                if (traceLogging) Logger.Trace("Get PathSeparator");
                return TestTreeListener.PathSeparator;
            }
            set
            {
                if (traceLogging) Logger.Trace("Set PathSeparator");
                TestTreeListener.PathSeparator = value;
            }
        }
    }

    [TestFixture]
    public class TreeBaseTests
    {
        [Test]
        public void ShouldClearOnEmptyData()
        {
            var testTree = new TestTree(true);
            var testTreeListener = testTree.TestTreeListener;

            testTreeListener.GetCollapsedFolders().Returns(new string[0]);
            testTreeListener.SelectedNode.Returns((TestTreeNode) null);
            testTreeListener.GetCheckedFiles().Returns(new string[0]);
            testTreeListener.Nodes.Returns(new List<TestTreeNode>());

            testTree.Load(Enumerable.Empty<TestTreeData>());
        
            testTreeListener.Received(1).OnClear();
            testTreeListener.SelectedNode.Set
        }
    }
}
