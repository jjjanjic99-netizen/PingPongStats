using Microsoft.Win32;
using PingPongStats.Core.ViewModels;
using System.IO;

namespace PingPongStats.App.Services;

/// <summary>Uses the native Windows folder picker (Microsoft.Win32.OpenFolderDialog,
/// available since .NET 8 for WPF).</summary>
public class WpfFolderPickerService : IFolderPickerService
{
    public string? PickFolder(string title, string? initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
