namespace PingPongStats.Core.ViewModels;

/// <summary>Abstraction over the Win32/WPF folder-picker dialog, so ViewModels
/// stay platform-agnostic and testable. Implemented in the WPF App project.</summary>
public interface IFolderPickerService
{
    string? PickFolder(string title, string? initialDirectory);
}

/// <summary>Abstraction over shell actions (opening Explorer at a path).
/// Implemented in the WPF App project.</summary>
public interface IShellService
{
    void OpenFolderInExplorer(string path);
}
