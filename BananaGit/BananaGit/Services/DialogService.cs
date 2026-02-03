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
    class DialogService(GitInfoViewModel? vm, GitService gitService)
    {
        private readonly GitService _gitService = gitService;
        
        private RemoteBranchViewModel _remoteBranchViewModel = new RemoteBranchViewModel(gitService);
        
        /// <summary>
        /// Opens a dialog for cloning a new repository 
        /// </summary>
        public void ShowCloneRepoDialog()
        {
            CloneRepoView view = new() { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
            view.ShowDialog();
        }

        /// <summary>
        /// Opens a dialog for entering github credentials
        /// </summary>
        public void ShowCredentialsDialog()
        {
            EnterCredentialsView view = new();
            view.ShowDialog();
        }

        /// <summary>
        /// Opens a dialog that shows all the remote git branches
        /// </summary>
        public void ShowRemoteBranchesDialog()
        {
            RemoteBranchView view = new() { 
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
            SettingsView view = new() { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
            view.Show();
        }
        /// <summary>
        /// Opens a dialog with a debug console
        /// </summary>
        public void ShowConsoleDialog(TerminalViewModel terminalViewModel)
        {
            TerminalView view = new() { DataContext = terminalViewModel, Owner = System.Windows.Application.Current.MainWindow };
            view.Show();
        }
    }
}
