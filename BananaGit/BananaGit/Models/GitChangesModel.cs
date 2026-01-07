using System.Diagnostics;
using BananaGit.Services;
using BananaGit.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;

namespace BananaGit.Models
{
    public partial class ChangedFile : ObservableObject
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public StatusEntry? StatusEntry { get; set; }
        
        private readonly GitService _gitService;

        public ChangedFile(GitService gitService)
        {
            _gitService = gitService;
            Name = string.Empty;
            FilePath = string.Empty;
        }
        public ChangedFile(GitService gitService, StatusEntry entry, string filePath)
        {
            _gitService = gitService;
            Name = filePath.GetName();
            FilePath = filePath;
            StatusEntry = entry;
        }
        
        /// <summary>
        /// Resets a specified file
        /// </summary>
        /// <param name="filePath"></param>
        [RelayCommand]
        private async Task ResetLocalFile(string filePath)
        {
            try
            {
                await _gitService.ResetLocalFileAsync(filePath);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
    }
}
