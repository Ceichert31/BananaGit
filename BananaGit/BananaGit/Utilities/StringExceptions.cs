using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
