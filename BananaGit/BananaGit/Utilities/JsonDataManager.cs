using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Saves the users personal github token to a local folder
        /// </summary>
        /// <param name="token">The users github token</param>
        public static void SaveGithubToken(string token)
        {
            TextWriter? writer = null;
            try
            {
                //Create directory before trying to write to file
                if (!Directory.Exists(USER_DATA_LOCATION))
                {
                    Directory.CreateDirectory(USER_DATA_LOCATION);
                }

                var contentsToWrite = JsonConvert.SerializeObject(token);
                writer = new StreamWriter(USER_DATA_LOCATION + USER_DATA_NAME, false);
                writer.Write(contentsToWrite);
            }
            finally
            {
                writer?.Close();
            }
        }

        public static string GetGithubToken()
        {
            TextReader? reader = null;
            string result = "";
            try
            {
                //If directory hasn't been created yet, return nothing
                if (!File.Exists(USER_DATA_LOCATION + USER_DATA_NAME))
                {
                    return string.Empty;
                }

                reader = new StreamReader(USER_DATA_LOCATION + USER_DATA_NAME);
                var fileContents = reader.ReadToEnd();
                result = JsonConvert.DeserializeObject<string>(fileContents) ?? "";

                if (result == "")
                {
                    HasPersonalToken = false;
                }
                else
                {
                    HasPersonalToken = true;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
            return result;
        }
    }
}
