using System.ComponentModel;
using System.Windows;
using BananaGit.EventArgExtensions;
using BananaGit.ViewModels;

namespace BananaGit.Views.DialogueViews
{
    /// <summary>
    /// Interaction logic for EnterCredentialsView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private readonly EventHandler<CredentialsEventArgs> _loginAttemptedEvent;

        private bool _isLoggedIn;

        public LoginView()
        {
            InitializeComponent();

            //Event to close credential dialogue
            _loginAttemptedEvent += UpdateLoginDialogue;

            var loginVm = new LoginViewModel(_loginAttemptedEvent);

            DataContext = loginVm;
        }

        /// <summary>
        /// Update login status and window status
        /// </summary>
        /// <param name="sender"><see cref="LoginViewModel"/></param>
        /// <param name="e"><see cref="CredentialsEventArgs"/></param>
        private void UpdateLoginDialogue(object? sender, CredentialsEventArgs e)
        {
            _isLoggedIn = e.LoginSuccess;

            if (!_isLoggedIn)
                return;

            Close();
        }

        /// <summary>
        /// Logic for handling user initiated window closure. If window is closed and while user isn't logged in, shutdown application. 
        /// </summary>
        /// <param name="sender">User</param>
        /// <param name="e"><see cref="CancelEventArgs"/></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_isLoggedIn) return;

            Application.Current.Shutdown();
        }
    }
}