using BananaGit.EventArgExtensions;
using BananaGit.Models;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels;

partial class CloneRepoViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _canClone;

    [ObservableProperty]
    private string _repositoryUrl = string.Empty;
    
    [ObservableProperty]
    private string _repositoryPath = string.Empty;

    private readonly GitService _gitService;
    private readonly GitInfoModel _gitInfo;
    
    public CloneRepoViewModel(GitService gitService, ref GitInfoModel gitInfo)
    {
        _gitService = gitService;
        _gitInfo = gitInfo;
    }
    
    [RelayCommand]
    private void CallRepositoryDialog()
    {
        var result = _gitService.ChooseRepositoryDialog();
        CanClone = result.Item2;
        RepositoryPath = result.Item1;
    }

    [RelayCommand]
    private async Task CloneRepository()
    {
        try
        {
            await _gitService.CloneRepository(RepositoryUrl, RepositoryPath);
            CanClone = false;
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
            CanClone = false;
        }
    }
}