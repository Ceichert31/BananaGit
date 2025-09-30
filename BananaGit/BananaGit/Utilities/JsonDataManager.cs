using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BananaGit.Utilities
{
    public static class JsonDataManager
    {
        public static bool HasPersonalToken = false;

        private const string USER_DATA_LOCATION = "C:\\BananaGit/";
        private const string USER_DATA_NAME = "UserInfo.txt";

        public static GithubUserInfo? UserInfo;

        public static void SetCurrentRepoURL(string url)
        {
            if (UserInfo == null) return;

            if (UserInfo?.SavedRepositories == null)
            {
                UserInfo.SavedRepositories = new();
                UserInfo?.SavedRepositories?.Add(new("", url));
                return;
            }

            UserInfo.SavedRepositories[0].URL = url;
        }
        public static void SetCurrentRepoFilePath(string filePath)
        {
            if (UserInfo == null) return;

            if (UserInfo?.SavedRepositories == null)
            {
                UserInfo.SavedRepositories = new();
                UserInfo?.SavedRepositories?.Add(new(filePath, ""));
                return;
            }

            UserInfo.SavedRepositories[0].FilePath = filePath;
        }

        public static void SaveGithubCredentials(string username, string personalToken)
        {
            UserInfo.Username = username;
            UserInfo.PersonalToken = personalToken;

            SaveUserInfo();
        }

        /// <summary>
        /// Saves the users personal github token to a local folder
        /// </summary>
        /// <param name="token">The users github token</param>
        private static void SaveUserInfo()
        {
            TextWriter? writer = null;

            //Create new user info if one doesn't already exsist
            UserInfo ??= new GithubUserInfo();
            try
            {
                //Create directory before trying to write to file
                if (!Directory.Exists(USER_DATA_LOCATION))
                {
                    Directory.CreateDirectory(USER_DATA_LOCATION);
                }

                string jsonString = JsonConvert.SerializeObject(UserInfo, Formatting.Indented);
                writer = new StreamWriter(USER_DATA_LOCATION + USER_DATA_NAME, false);
                writer.Write(jsonString);
            }
            finally
            {
                writer?.Close();
            }
        }

        public static void LoadUserInfo()
        {
            TextReader? reader = null;
            try
            {
                //If directory hasn't been created yet, return nothing
                if (!File.Exists(USER_DATA_LOCATION + USER_DATA_NAME))
                {
                    HasPersonalToken = false;
                    return;
                }

                reader = new StreamReader(USER_DATA_LOCATION + USER_DATA_NAME);
                var fileContents = reader.ReadToEnd();
                GithubUserInfo userInfo = JsonConvert.DeserializeObject<GithubUserInfo>(fileContents) ?? throw new NullReferenceException();
                UserInfo = userInfo; 
                HasPersonalToken = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }

        /// <summary>
        /// Save a cloned repo to user info
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="repoURL"></param>
        public static void SaveRepositoryInformation(string localFilePath, string repoURL)
        {
            SaveableRepository repo = new(localFilePath, repoURL);
            UserInfo?.SavedRepositories?.Add(repo);

            SaveUserInfo();
        }
    }

    public class GithubUserInfo
    {
        public string? Username { get; set; }
        public string? PersonalToken { get; set; }

        public List<SaveableRepository>? SavedRepositories { get; set; }
    }
    public class SaveableRepository(string path, string url)
    {
        public string FilePath { get; set; } = path;
        public string URL { get; set; } = url;
        public void SetURL(string url)
        {
            URL = url;
        }
        public void SetFilePath(string filePath)
        {
            FilePath = filePath;
        }
    }
}
