using System.Diagnostics;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Services;

public class WpfShellService : IShellService
{
    public void OpenFolderInExplorer(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = true,
        });
    }
}
