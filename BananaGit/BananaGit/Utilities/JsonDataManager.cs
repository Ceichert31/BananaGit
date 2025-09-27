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

        /// <summary>
        /// Saves the users personal github token to a local folder
        /// </summary>
        /// <param name="token">The users github token</param>
        public static void SaveGithubToken(string token)
        {
            TextWriter? writer = null;
            try
            {
                var contentsToWrite = JsonConvert.SerializeObject(token);
                writer = new StreamWriter("C:\\BananaGit/UserData.txt", false);
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
                if (!File.Exists("C:\\BananaGit/UserData.txt"))
                {
                    SaveGithubToken("");
                }
                reader = new StreamReader("C:\\BananaGit/UserData.txt");
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
