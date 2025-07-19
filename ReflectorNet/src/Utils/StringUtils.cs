
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class StringUtils
    {
        static Dictionary<int, string> _paddingCache = new Dictionary<int, string>();

        public static string GetPadding(int depth)
        {
            if (depth < 0)
                return string.Empty;

            if (_paddingCache.TryGetValue(depth, out var padding))
                return padding;

            padding = new string(' ', depth);
            _paddingCache[depth] = padding;
            return padding;
        }

        public static string? TrimPath(string? path)
            => path?.TrimEnd('/')?.TrimStart('/');

        public static bool Path_ParseParent(string? path, out string? parentPath, out string? name)
        {
            path = TrimPath(path);
            if (string.IsNullOrEmpty(path))
            {
                parentPath = null;
                name = null;
                return false;
            }

            var lastSlashIndex = path!.LastIndexOf('/');
            if (lastSlashIndex >= 0)
            {
                parentPath = path.Substring(0, lastSlashIndex);
                name = path.Substring(lastSlashIndex + 1);
                return true;
            }
            else
            {
                parentPath = null;
                name = path;
                return false;
            }
        }
        public static string? Path_GetParentFolderPath(string? path)
        {
            if (path == null)
                return null;
            var trimmedPath = path.TrimEnd('/');
            var lastSlashIndex = trimmedPath.LastIndexOf('/');
            return lastSlashIndex >= 0 ? trimmedPath.Substring(0, lastSlashIndex) : trimmedPath;
        }
        public static string? Path_GetLastName(string? path)
            => path?.TrimEnd('/')?.Split('/')?.Last();
    }
}