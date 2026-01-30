using System.IO;
using BananaGit.Models;
using Newtonsoft.Json;

namespace BananaGit.Utilities
{
    public class JsonDataManager
    {
        public static bool HasPersonalToken = false;

        private const string USER_DATA_LOCATION = "C:\\ProgramData/BananaGit/";
        private const string USER_DATA_NAME = "UserInfo.json";

        public static EventHandler? UserInfoChanged;

        /// <summary>
        /// Saves the users personal github token to a local folder
        /// </summary>
        /// <param name="token">The users github token</param>
        public static void SaveUserInfo(GitInfoModel? userInfo)
        {
            TextWriter? writer = null;

            if (userInfo == null) return;
            
            try
            {
                //Create directory before trying to write to file
                if (!Directory.Exists(USER_DATA_LOCATION))
                {
                    Directory.CreateDirectory(USER_DATA_LOCATION);
                }

                string jsonString = JsonConvert.SerializeObject(userInfo, Formatting.Indented);
                writer = new StreamWriter(USER_DATA_LOCATION + USER_DATA_NAME, false);
                writer.Write(jsonString);
            }
            finally
            {
                writer?.Close();
                UserInfoChanged?.Invoke(nameof(JsonDataManager), EventArgs.Empty);
            }
        }

        /// <summary>
        /// Loads the github user info into the passed in user info variable
        /// </summary>
        /// <param name="userInfo">The variable where the user info is drawn into</param>
        public static void LoadUserInfo(ref GitInfoModel? userInfo)
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

                //If data couldn't be loaded, convert github info to null
                GitInfoModel? loadedInfo = JsonConvert.DeserializeObject<GitInfoModel>(fileContents) ?? null;
                userInfo = loadedInfo; 
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
    }
}
