using BananaGit.ViewModels;
using BananaGit.Views;
using BananaGit.Views.DialogueViews;

namespace BananaGit.Utilities
{
    class DialogService : IDialogService
    {
        public bool ShowCloneRepoDialog(GitInfoViewModel? vm)
        {
            CloneRepoView view = new() { DataContext = vm, Owner = App.Current.MainWindow };

            return view.ShowDialog() ?? false;
        }

        public bool ShowCredentialsDialog(GitInfoViewModel? vm)
        {
            EnterCredentialsView view = new() { DataContext = vm };

            return view.ShowDialog() ?? false;
        }

        public bool ShowRemoteBranchesDialog(GitInfoViewModel? vm)
        {
            RemoteBranchView view = new() { DataContext = vm, Owner = App.Current.MainWindow };
            view.Show();
            return true;
        }
    }

    interface IDialogService
    {
        public bool ShowCredentialsDialog(GitInfoViewModel? vm);
        public bool ShowCloneRepoDialog(GitInfoViewModel? vm);
        public bool ShowRemoteBranchesDialog(GitInfoViewModel? vm);
    }
}
