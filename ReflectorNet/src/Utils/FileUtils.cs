using System.IO;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class FileUtils
    {
        public static bool FileExistsWithoutExtension(string directoryPath, string fileNameWithoutExtension)
        {
            if (!Directory.Exists(directoryPath))
                return false;

            return Directory.GetFiles(directoryPath, $"{fileNameWithoutExtension}*").Any();
        }
        public static string? ReadFileContent(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            try
            {
                return File.ReadAllText(filePath);
            }
            catch
            {
                return null;
            }
        }
    }
}