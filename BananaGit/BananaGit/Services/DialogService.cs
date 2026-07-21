using BananaGit.Exceptions;
using BananaGit.ViewModels;
using BananaGit.ViewModels.DialogueViewModels;
using BananaGit.Views;
using BananaGit.Views.DialogueViews;

namespace BananaGit.Services
{
    /// <summary>
    /// The base dialog service that manages creating views with viewmodels that don't use <see cref="GitService"/>
    /// </summary>
    public class DialogService
    {
        /// <summary>
        /// Opens a dialog for entering github credentials
        /// </summary>
        public void ShowCredentialsDialog()
        {
            LoginView view = new();
            view.ShowDialog();
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
    }
}