using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    static class FileSystemHelpers
    {
        public static string FindCommonPath(IEnumerable<string> paths)
        {
            var parentPaths = paths.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.ToNPath().Parent);
            if (!parentPaths.Any())
                return null;

            var parentsArray = parentPaths.ToArray();
            var maxDepth = parentsArray.Max(path => path.Depth);
            var deepestPath = parentsArray.First(path => path.Depth == maxDepth);

            var commonParent = deepestPath;
            foreach (var path in parentsArray)
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