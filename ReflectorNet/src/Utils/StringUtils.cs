#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class StringUtils
    {
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

            var lastSlashIndex = path.LastIndexOf('/');
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