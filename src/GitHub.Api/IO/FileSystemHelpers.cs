using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    static class FileSystemHelpers
    {
        public static string FindCommonPath(IEnumerable<string> paths)
        {
            var parentPaths = paths.Where(s => s != null).Select(s => s.ToNPath().Parent).ToArray();
            if (!parentPaths.Any())
                return null;

            var maxDepth = parentPaths.Max(path => path.Depth);
            var deepestPath = parentPaths.First(path => path.Depth == maxDepth);

            var commonParent = deepestPath;
            foreach (var path in parentPaths)
            {
                var cp = path.Elements.Any() ? commonParent.GetCommonParent(path) : null;
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
    }
}