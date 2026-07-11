using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PingPongStats.Core.Services;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Services;

/// <summary>
/// WPF-side avatar image processing: decode, center-crop to a square, cap at
/// AvatarService.MaxAvatarPixelSize, and re-encode as PNG. This is platform
/// imaging IO (needs WPF's imaging stack), not business logic, which is why
/// it lives here rather than in PingPongStats.Core - see IAvatarImageService.
/// </summary>
public class AvatarImageService : IAvatarImageService
{
    public string SaveAvatar(string sourceImagePath, string dataPath, Guid playerId)
    {
        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.UriSource = new Uri(sourceImagePath, UriKind.Absolute);
        source.EndInit();
        source.Freeze();

        var squareSize = Math.Min(source.PixelWidth, source.PixelHeight);
        var xOffset = (source.PixelWidth - squareSize) / 2;
        var yOffset = (source.PixelHeight - squareSize) / 2;
        var cropped = new CroppedBitmap(source, new Int32Rect(xOffset, yOffset, squareSize, squareSize));

        BitmapSource finalImage = cropped;
        if (squareSize > AvatarService.MaxAvatarPixelSize)
        {
            var scale = (double)AvatarService.MaxAvatarPixelSize / squareSize;
            finalImage = new TransformedBitmap(cropped, new ScaleTransform(scale, scale));
        }

        var avatarsDir = AvatarService.GetAvatarsDirectory(dataPath);
        Directory.CreateDirectory(avatarsDir);
        var targetPath = AvatarService.GetAvatarFilePath(dataPath, playerId);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(finalImage));

        // Write to a temp file first, then replace, so a failed/partial write never
        // corrupts a pre-existing avatar.
        var tempPath = targetPath + ".tmp";
        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
        {
            encoder.Save(stream);
        }

        if (File.Exists(targetPath)) File.Delete(targetPath);
        File.Move(tempPath, targetPath);

        return $"{playerId}.png";
    }
}
