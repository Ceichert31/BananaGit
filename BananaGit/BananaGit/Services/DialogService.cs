using BananaGit.Models;
using BananaGit.ViewModels;
using BananaGit.Views;
using BananaGit.Views.DialogueViews;

namespace BananaGit.Services
{
    /// <summary>
    /// Manages views and creates dialogs
    /// </summary>
    /// <param name="vm">The <see cref="GitInfoViewModel"/>
    /// that is currently being used by the main window </param>
    class DialogService
    {
        private readonly RemoteBranchViewModel _remoteBranchViewModel;
        private readonly CloneRepoViewModel _cloneRepoViewModel;
        private readonly DiscardChangesViewModel _discardChangesViewModel;

        public DialogService(GitService gitService, GitInfoModel gitInfo)
        {
            _remoteBranchViewModel = new RemoteBranchViewModel(gitService);
            _cloneRepoViewModel = new CloneRepoViewModel(gitService, gitInfo);
            _discardChangesViewModel = new DiscardChangesViewModel(gitService, this);
        }

        /// <summary>
        /// Opens a dialog for cloning a new repository 
        /// </summary>
        public void ShowCloneRepoDialog()
        {
            CloneRepoView view = new()
                { DataContext = _cloneRepoViewModel, Owner = System.Windows.Application.Current.MainWindow };
            view.ShowDialog();
        }

        /// <summary>
        /// Opens a dialog for entering github credentials
        /// </summary>
        public void ShowCredentialsDialog()
        {
            LoginView view = new();
            view.ShowDialog();
        }

        /// <summary>
        /// Opens a dialog that shows all the remote git branches
        /// </summary>
        public void ShowRemoteBranchesDialog()
        {
            RemoteBranchView view = new()
            {
                DataContext = _remoteBranchViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };
            view.Show();
        }

        /// <summary>
        /// Opens a dialog that shows all the user settings
        /// </summary>
        public void ShowSettingsDialog()
        {
            SettingsView view = new()
                { DataContext = new SettingsViewModel(), Owner = System.Windows.Application.Current.MainWindow };
            view.ShowDialog();
        }

        /// <summary>
        /// Opens a dialog with a debug console
        /// </summary>
        public void ShowConsoleDialog(TerminalViewModel terminalViewModel)
        {
            TerminalView view = new()
                { DataContext = terminalViewModel, Owner = System.Windows.Application.Current.MainWindow };
            view.Show();
        }

        private DiscardChangesConfirmationView? discardChangesView;

        /// <summary>
        /// Opens a dialog confirming discarding local changes
        /// </summary>
        public void ShowDiscardChangesDialog()
        {
            discardChangesView = new()
            {
                DataContext = _discardChangesViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };
            discardChangesView.ShowDialog();
        }

        public void CloseDiscardChangesDialog()
        {
            discardChangesView?.Close();
        }
    }
}