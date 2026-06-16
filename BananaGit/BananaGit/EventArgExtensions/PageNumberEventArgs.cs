using BananaGit.ViewModels;

namespace BananaGit.EventArgExtensions;

/// <summary>
/// Used to update pages on the current page number in <see cref="CommitHistoryViewModel"/>
/// </summary>
public class PageNumberEventArgs : EventArgs
{
    public PageNumberEventArgs(uint pageIndex)
    {
        PageNumber = pageIndex;
    }

    public uint PageNumber { get; set; } = 0;
}