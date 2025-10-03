namespace BananaGit.Models
{
    public class GitInfoModel
    {
        public GitInfoModel() 
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

        /// <summary>
        /// Returns whether the repo has actual data or is just empty
        /// </summary>
        /// <returns></returns>
        public bool IsValidRepository()
        {
            if (FilePath == null || URL == null)
                return false;
            if (FilePath.Equals(string.Empty) || URL.Equals(string.Empty))
                return false;

            return true;
        }
    }
    public class GitCommitInfo
    {
        public string? Author { get; set; }
        public string? Date { get; set; }
        public string? Message { get; set; }
        public string? Commit { get; set; }
    }
}
