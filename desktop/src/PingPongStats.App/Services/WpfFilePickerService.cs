using Microsoft.Win32;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Services;

/// <summary>Uses the native Windows file picker, restricted to common image formats
/// (used for avatar selection).</summary>
public class WpfFilePickerService : IFilePickerService
{
    public string? PickImageFile(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "Bilder (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            CheckFileExists = true,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
