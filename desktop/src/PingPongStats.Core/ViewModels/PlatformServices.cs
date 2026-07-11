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

/// <summary>Abstraction over the Win32/WPF file-picker dialog (used for avatar image
/// selection), so ViewModels stay platform-agnostic. Implemented in the WPF App project.</summary>
public interface IFilePickerService
{
    /// <summary>Opens a file picker restricted to image files (jpg/png). Returns the
    /// chosen absolute path, or null if the user cancelled.</summary>
    string? PickImageFile(string title);
}

/// <summary>
/// Abstraction over avatar image processing (decode, center-crop to square, resize,
/// re-encode as PNG). This is platform imaging IO, not a calculation, so - like
/// IFolderPickerService/IShellService - it is implemented in the WPF App project
/// using WPF's imaging APIs and injected here as an interface.
/// </summary>
public interface IAvatarImageService
{
    /// <summary>Processes the source image (any common format) into a square,
    /// max-512x512 PNG saved at {dataPath}/avatars/{playerId}.png, overwriting any
    /// existing avatar for that player. Returns the file name to store in
    /// Player.AvatarFileName (always "{playerId}.png").</summary>
    string SaveAvatar(string sourceImagePath, string dataPath, Guid playerId);
}
