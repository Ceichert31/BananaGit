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
        private readonly EventHandler openCloneWindow;

        private GitInfoViewModel? gitInfoVM;
        private EnterCredentialsView? enterCredentialsView;
        private CloneRepoView? cloneRepoView;

        private GithubUserInfo? userInfo = new();

        public MainWindow()
        {
            InitializeComponent();

            Show();

            cloneRepoView = new CloneRepoView();
            cloneRepoView.Owner = this;
           
            openCloneWindow += OpenCloneWindow;

            gitInfoVM = new(openCloneWindow);

            DataContext = gitInfoVM;
            cloneRepoView.DataContext = gitInfoVM;

            //Load user info
            JsonDataManager.LoadUserInfo(ref userInfo);

            //If no user info could be loaded
            if (userInfo == null)
            {
                enterCredentialsView = new EnterCredentialsView();
                enterCredentialsView.Owner = this;
                enterCredentialsView.ShowDialog();
            }
        }
        private void OpenCloneWindow(object? sender, EventArgs e)
        {
            cloneRepoView = new();
            cloneRepoView?.ShowDialog();
        }
    }
}