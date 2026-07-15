using System.Text;
using System.Text.Json;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using System.IO;

namespace PingPongStats.Core.Repositories;

public interface ISettingsRepository
{
    AppSettingsModel Load();
    void Save(AppSettingsModel settings);
    string SettingsFilePath { get; }
}

/// <summary>
/// Persists the per-user local app configuration (which data path to use, UI
/// preferences) as JSON under %AppData%\PingPongStats\appsettings.json. This
/// is intentionally local, not shared - every user picks/points at the same
/// network DataPath independently.
/// </summary>
public class SettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsRepository(string? settingsFilePath = null)
    {
        SettingsFilePath = settingsFilePath ?? GetDefaultSettingsFilePath();
    }

    public string SettingsFilePath { get; }

    public static string GetDefaultSettingsFilePath()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PingPongStats");
        return Path.Combine(appDataDir, "appsettings.json");
    }

    public AppSettingsModel Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new AppSettingsModel();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<AppSettingsModel>(json, JsonOptions) ?? new AppSettingsModel();
        }
        catch (Exception ex)
        {
            Logger.Warn($"Einstellungsdatei \"{SettingsFilePath}\" konnte nicht gelesen werden, verwende Standardwerte: {ex.Message}");
            return new AppSettingsModel();
        }
    }

    public void Save(AppSettingsModel settings)
    {
        var directory = Path.GetDirectoryName(SettingsFilePath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
