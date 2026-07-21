using BananaGit.ViewModels;
using BananaGit.ViewModels.DialogueViewModels;
using BananaGit.Views;
using BananaGit.Views.DialogueViews;

namespace BananaGit.Services;

public class GitDialogService : DialogService
{
    private readonly GitService _gitService;

    private RemoteBranchViewModel? _remoteBranchViewModel;
    private CloneRepoViewModel? _cloneRepoViewModel;
    private DiscardChangesViewModel? _discardChangesViewModel;
    private CreateBranchViewModel? _createBranchViewModel;

    private CreateBranchView? _createBranchView;
    private DiscardChangesConfirmationView? _discardChangesView;

    public GitDialogService(GitService gitService)
    {
        _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
    }

    /// <summary>
    /// Opens a dialog for cloning a new repository 
    /// </summary>
    public void ShowCloneRepoDialog()
    {
        _cloneRepoViewModel ??= new CloneRepoViewModel(_gitService);

        CloneRepoView view = new()
            { DataContext = _cloneRepoViewModel, Owner = System.Windows.Application.Current.MainWindow };
        view.ShowDialog();
    }


    /// <summary>
    /// Opens a dialog that shows all the remote git branches
    /// </summary>
    public void ShowRemoteBranchesDialog()
    {
        _remoteBranchViewModel ??= new RemoteBranchViewModel(_gitService);

        RemoteBranchView view = new()
        {
            DataContext = _remoteBranchViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };
        view.Show();
    }

    /// <summary>
    /// Opens a dialog confirming discarding local changes
    /// </summary>
    public void ShowDiscardChangesDialog()
    {
        _discardChangesViewModel ??= new DiscardChangesViewModel(_gitService, this);

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
        _createBranchViewModel ??= new CreateBranchViewModel(_gitService, this);

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