using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels;

partial class TutorialViewModel : ObservableObject
{
    private readonly DialogService? _dialogService;
    private readonly GitService? _gitService;

    public TutorialViewModel(DialogService dialogService, GitService gitService)
    {
        _dialogService = dialogService;
        _gitService = gitService;
    }

    
}