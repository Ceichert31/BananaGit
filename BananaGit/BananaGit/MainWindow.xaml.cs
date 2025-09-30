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

        private GithubUserInfo? userInfo = new();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = gitInfoVM;

            //Load user info
            JsonDataManager.LoadUserInfo(ref userInfo);

            //If no user info could be loaded
            if (userInfo == null)
            {
                enterCredentialsView.ShowDialog();
                //enterCredentialsView.Owner = this;
            }
        }

        
    }
}