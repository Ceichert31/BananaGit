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

     
        private EnterCredentialsView? enterCredentialsView;
        private CloneRepoView? cloneRepoView;
        private GitChangesView? gitChangesView;

        private GitInfoViewModel? gitInfoVM;
        private GithubUserInfo? userInfo;


        public MainWindow()
        {
            InitializeComponent();

            Show();

            //Load user info
            JsonDataManager.LoadUserInfo(ref userInfo);

            //If no user info could be loaded
            if (userInfo == null)
            {
                enterCredentialsView = new EnterCredentialsView();
                enterCredentialsView.Owner = this;
                enterCredentialsView.ShowDialog();
            }
           
            openCloneWindow += OpenCloneWindow;

            gitInfoVM = new(openCloneWindow);

            DataContext = gitInfoVM;

            gitChangesView = new GitChangesView();
            gitChangesView.DataContext = gitInfoVM;
            GitChangesContent.Content = gitChangesView;
        }
        private void OpenCloneWindow(object? sender, EventArgs e)
        {
            cloneRepoView = new();
            cloneRepoView.DataContext = gitInfoVM;
            cloneRepoView.Owner = this;
            cloneRepoView?.ShowDialog();
        }
    }
}