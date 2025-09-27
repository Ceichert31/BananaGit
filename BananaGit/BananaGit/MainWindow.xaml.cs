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

            string token = JsonDataManager.GetGithubToken();

            //Display prompt to enter personal token
            if (token == "")
            {
                enterCredentialsView.ShowDialog();
                //enterCredentialsView.Owner = this;
            }
        }
    }
}