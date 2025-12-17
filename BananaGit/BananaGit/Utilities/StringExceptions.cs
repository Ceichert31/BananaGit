using System.IO;

namespace BananaGit.Utilities
{
    public static class StringExceptions
    {
        public static string GetName(this string filePath)
        {
            return Path.GetFileName(filePath);
        }
    }
}
