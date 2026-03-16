using System.Diagnostics;
using BananaGit.EventArgExtensions;
using BananaGit.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BananaGit.ViewModels;

/// <summary>
/// Backend for confirmation popup
/// </summary>
/// <remarks>
/// The goal of this script is to handle methods for the confirmation popup,
/// like executing the discard command or closing the window
/// </remarks>
partial class DiscardChangesViewModel : ObservableObject
{
    private readonly GitService _gitService;
    private readonly DialogService _dialogService;
    
    public DiscardChangesViewModel(GitService gitService, DialogService dialogService)
    {
        _gitService = gitService;
        _dialogService = dialogService; 
    }
    
    /// <summary>
    /// Calls git service to discard all local changes (Not including local commits)
    /// </summary>
    [RelayCommand]
    private async Task DiscardLocalChanges()
    {
        try
        {
            _dialogService.CloseDiscardChangesDialog();
            await _gitService.ResetLocalUncommittedFilesAsync();
   
            Trace.WriteLine("Discarded Local Changes!");
        }
        catch (Exception ex)
        {
            _gitService.OutputToConsole(this, new MessageEventArgs(ex.Message));
        }
    }
    
    [RelayCommand]
    private void CloseDialog() => _dialogService.CloseDiscardChangesDialog();
}