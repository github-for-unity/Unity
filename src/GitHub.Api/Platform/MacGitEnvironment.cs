using System.Threading.Tasks;
using GitHub.Unity;
using System;

namespace GitHub.Unity
{
    class MacGitEnvironment : GitProcessEnvironment
    {
        public MacGitEnvironment(IEnvironment environment, IFileSystem filesystem)
            : base(environment, filesystem)
        {
        }

        public override string GetExecutableExtension()
        {
            return null;
        }
    }
}