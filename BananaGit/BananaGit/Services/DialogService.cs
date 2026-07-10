using BananaGit.ViewModels;
using BananaGit.ViewModels.DialogueViewModels;
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
        private readonly CreateBranchViewModel _createBranchViewModel;

        private CreateBranchView? _createBranchView;
        private DiscardChangesConfirmationView? _discardChangesView;

        public DialogService(GitService gitService)
        {
            _remoteBranchViewModel = new RemoteBranchViewModel(gitService);
            _cloneRepoViewModel = new CloneRepoViewModel(gitService);
            _discardChangesViewModel = new DiscardChangesViewModel(gitService, this);
            _createBranchViewModel = new CreateBranchViewModel(gitService, this);
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

        /// <summary>
        /// Opens a dialog confirming discarding local changes
        /// </summary>
        public void ShowDiscardChangesDialog()
        {
            _discardChangesView = new()
            {
                DataContext = _discardChangesViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };
            _discardChangesView.ShowDialog();
        }

        public void CloseDiscardChangesDialog()
        {
            _discardChangesView?.Close();
        }

        public void ShowCreateBranchDialog()
        {
            _createBranchView = new()
            {
                DataContext = _createBranchViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };
            _createBranchView.ShowDialog();
        }

        public void CloseCreateBranchDialog()
        {
            _createBranchView?.Close();
        }
    }
}