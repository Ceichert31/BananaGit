using BananaGit.Models;
using BananaGit.Utilities;
using BananaGit.Views;

namespace BananaGit.ViewModels
{
    class MainWindowViewModel
    {
        public EnterCredentialsView? EnterCredentialsView { get; set; }
        public CloneRepoView? CloneRepoView { get; set; }
        public ToolbarView? ToolbarView { get; set; }
        public GitChangesView? GitChangesView { get; set; }
        public CommitView? CommitView { get; set; }
        public CommitHistoryView? CommitHistoryView { get; set; }

        private readonly EventHandler openCloneWindow;

        private readonly GitInfoViewModel? gitInfoVM;
        private readonly GithubUserInfo? userInfo;

        public MainWindowViewModel() 
        {
            //Load user info
            JsonDataManager.LoadUserInfo(ref userInfo);

            //If no user info could be loaded
            if (userInfo == null)
            {
                EnterCredentialsView = new EnterCredentialsView()
                {
                    //Owner = App.Current.MainWindow
                };
                EnterCredentialsView.ShowDialog();
            }

            openCloneWindow += OpenCloneWindow;

            gitInfoVM = new(openCloneWindow);

            //Setup user controls for main window
            ToolbarView = new ToolbarView()
            {
                DataContext = gitInfoVM
            };
            GitChangesView = new GitChangesView
            {
                DataContext = gitInfoVM
            };
            CommitView = new CommitView
            {
                DataContext = gitInfoVM
            };
            CommitHistoryView = new CommitHistoryView
            {
                DataContext = gitInfoVM
            };
        }
        private void OpenCloneWindow(object? sender, EventArgs e)
        {
            CloneRepoView = new()
            {
                DataContext = gitInfoVM,
                Owner = App.Current.MainWindow
            };
            CloneRepoView?.ShowDialog();
        }
    }
}
