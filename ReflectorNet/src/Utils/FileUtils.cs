#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.IO;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet
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