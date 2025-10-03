using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BananaGit.Models
{
    public class GithubUserInfo
    {
        public GithubUserInfo() 
        {
            Username = string.Empty;
            PersonalToken = string.Empty;
            SavedRepository = new(string.Empty, string.Empty);
        }
        public string? Username { get; set; }
        public string? PersonalToken { get; set; }
        public SaveableRepository? SavedRepository { get; set; }
    }
    public class SaveableRepository(string path, string url)
    {
        public string FilePath { get; set; } = path;
        public string URL { get; set; } = url;
    }
}
