using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BananaGit.Utilities
{
    public class GithubUserInfo
    {
        public string? Username { get; set; }
        public string? PersonalToken { get; set; }
        public List<SaveableRepository> SavedRepositories { get; set; } = new();
    }
    public class SaveableRepository(string path, string url)
    {
        public string FilePath { get; set; } = path;
        public string URL { get; set; } = url;
    }
}
