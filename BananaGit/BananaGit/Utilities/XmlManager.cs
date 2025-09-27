using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Octokit;

namespace BananaGit.Utilities
{
    static public class XmlManager
    {
        /// <summary>
        /// Saves the users personal github credential
        /// </summary>
        /// <param name="credentials">The personal credential</param>
        public static void SaveGithubCredentials(string credentials)
        {
            //XmlTextReader textReader = new XmlTextReader(Environment.CurrentDirectory + "credential.txt");
            XmlTextWriter textWriter = new(Environment.CurrentDirectory + "credential.txt", null); 

            textWriter.WriteStartDocument();
            textWriter.WriteComment(credentials);
        }

       /* public static string GetCredentials()
        {
            XmlDocument document = new XmlDocument();
            XmlTextReader textReader = new XmlTextReader(Environment.CurrentDirectory + "credential.txt");

            textReader.Read();
         
            document.Load(textReader);

            Stream stream;
            document.Save();

            return null;
        }*/
    }
}
