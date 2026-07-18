using System.IO;

namespace BananaGit.Utilities
{
    public static class StringExtensions
    {
        public static string GetName(this string filePath)
        {
            return Path.GetFileName(filePath);
        }
    }
}