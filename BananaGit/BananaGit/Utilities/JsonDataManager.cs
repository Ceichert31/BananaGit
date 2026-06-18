using System.IO;
using BananaGit.Models;
using Newtonsoft.Json;

namespace BananaGit.Utilities
{
    /// <summary>
    /// Manages saving and loading data locally on the users computer
    /// </summary>
    public static class JsonDataManager
    {
        public static bool HasPersonalToken = false;

        public static EventHandler? OnUserInfoChanged;

        private const string UserDataLocation = "C:\\ProgramData/BananaGit/";
        private const string UserDataName = "UserInfo.json";

        /// <summary>
        /// Saves the Users GitHub credentials locally
        /// </summary>
        /// <param name="userInfo">The class that holds all the users GitHub information</param>
        public static void SaveUserInfo(GitInfoModel? userInfo)
        {
            TextWriter? writer = null;

            if (userInfo == null) return;

            try
            {
                //Create directory before trying to write to file
                if (!Directory.Exists(UserDataLocation))
                {
                    Directory.CreateDirectory(UserDataLocation);
                }

                string jsonString = JsonConvert.SerializeObject(userInfo, Formatting.Indented);
                writer = new StreamWriter(UserDataLocation + UserDataName, false);
                writer.Write(jsonString);
            }
            finally
            {
                writer?.Close();
                OnUserInfoChanged?.Invoke(nameof(JsonDataManager), EventArgs.Empty);
            }
        }

        /// <summary>
        /// Loads the GitHub user info into the passed in user info variable
        /// </summary>
        /// <param name="userInfo">The variable where the user info is drawn into</param>
        public static void LoadUserInfo(ref GitInfoModel? userInfo)
        {
            TextReader? reader = null;

            try
            {
                //If directory hasn't been created yet, return nothing
                if (!File.Exists(UserDataLocation + UserDataName))
                {
                    HasPersonalToken = false;
                    throw new IOException("UserInfo.json is missing");
                }

                reader = new StreamReader(UserDataLocation + UserDataName);
                var fileContents = reader.ReadToEnd();

                if (string.IsNullOrEmpty(fileContents))
                {
                    HasPersonalToken = false;
                    throw new IOException("UserInfo.json is empty!");
                }

                //If data couldn't be loaded, convert GitHub info to null
                GitInfoModel? loadedInfo = JsonConvert.DeserializeObject<GitInfoModel>(fileContents) ?? null;
                userInfo = loadedInfo;
                HasPersonalToken = true;
            }
            finally
            {
                reader?.Close();
            }
        }
    }
}