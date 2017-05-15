using System.Collections.Generic;
using System.Linq;
using GitHub.Unity;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Rackspace.Threading;
using TestUtils;

namespace IntegrationTests
{
    [TestFixture]
    class LoginIntegrationTests : BaseGitEnvironmentTest
    {
        string FindCommonPath(IEnumerable<string> paths)
        {
            var longestPath =
                paths.First(first => first.Length == paths.Max(second => second.Length))
                .ToNPath();

            NPath commonParent = longestPath;
            foreach (var path in paths)
            {
                var cp = commonParent.GetCommonParent(path);
                if (cp != null)
                    commonParent = cp;
                else
                {
                    commonParent = null;
                    break;
                }
            }
            return commonParent;
        }

        [Test]
        public void CommonParentTest()
        {
            var filesystem = new FileSystem(TestRepoMasterDirtyUnsynchronized);
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();

            var ret = FindCommonPath(new string[]
            {
                "Assets/Test/Path/file",
                "Assets/Test/something",
                "Assets/Test/Path/another",
                "Assets/alkshdsd",
                "Assets/Test/sometkjh",
            });

            Assert.AreEqual("Assets", ret);
        }
    }
}
