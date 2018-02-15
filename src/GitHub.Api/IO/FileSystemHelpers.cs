using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    static class FileSystemHelpers
    {
        public static string FindCommonPath(IEnumerable<string> paths)
        {
            var parentPaths = paths.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.ToNPath().Parent).ToArray();
            if (parentPaths.Count() <= 1)
                return null;

            var maxDepth = parentPaths.Max(path => path.Depth);
            var deepestPath = parentPaths.First(path => path.Depth == maxDepth);

            var commonParent = deepestPath;
            foreach (var path in parentPaths)
            {
                commonParent = path.Elements.Any() ? commonParent.GetCommonParent(path) : NPath.Default;
                if (!commonParent.IsInitialized)
                    break;
            }
            return commonParent;
        }
    }
}