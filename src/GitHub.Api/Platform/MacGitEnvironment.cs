using System.Threading.Tasks;
using GitHub.Unity;
using System;

namespace GitHub.Unity
{
    class MacGitEnvironment : GitEnvironment
    {
        public MacGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
        {
        }

        public override string GetGitExecutableExtension()
        {
            return null;
        }
    }
}