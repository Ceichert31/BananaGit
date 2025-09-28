using System.Windows;
using BananaGit.ViewModels;
using BananaGit.Utilities;
using BananaGit.Views;

namespace BananaGit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GitInfoViewModel gitInfoVM = new();
        private EnterCredentialsView enterCredentialsView = new();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = gitInfoVM;

            //Get token and check if it has been previously saved
            JsonDataManager.LoadGithubCredentials();

            //Display prompt to enter personal token
            if (JsonDataManager.UserInfo == null)
            {
                enterCredentialsView.ShowDialog();
                //enterCredentialsView.Owner = this;
            }
        }
    }
}