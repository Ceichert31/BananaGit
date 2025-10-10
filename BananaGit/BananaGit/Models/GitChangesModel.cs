using BananaGit.Utilities;
using LibGit2Sharp;

namespace BananaGit.Models
{
    class ChangedFile
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public StatusEntry? StatusEntry { get; set; }

        public ChangedFile()
        {
            Name = string.Empty;
            FilePath = string.Empty;
        }
        public ChangedFile(StatusEntry entry, string filePath)
        {
            Name = filePath.GetName();
            FilePath = filePath;
            StatusEntry = entry;
        }
    }
}
