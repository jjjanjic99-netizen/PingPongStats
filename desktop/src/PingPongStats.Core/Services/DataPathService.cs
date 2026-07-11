using PingPongStats.Core.Helpers;
using PingPongStats.Core.Repositories;

namespace PingPongStats.Core.Services;

/// <summary>
/// Validates and bootstraps a data path (local folder or UNC network share):
/// ensures the folder, the backups subfolder, and the required XML files
/// exist, and verifies the path is actually reachable and writable before the
/// rest of the application relies on it.
/// </summary>
public class DataPathService
{
    /// <summary>
    /// Throws <see cref="DataPathUnavailableException"/> with a user-facing German
    /// message if the path cannot be created/reached or is not writable.
    /// Creates missing folders/XML files otherwise (idempotent - safe to call
    /// on every startup and whenever the user changes the path in Settings).
    /// </summary>
    public void ValidateAndPrepare(string dataPath)
    {
        if (string.IsNullOrWhiteSpace(dataPath))
        {
            throw new DataPathUnavailableException("Es wurde kein Datenpfad angegeben.");
        }

        try
        {
            Directory.CreateDirectory(dataPath);
        }
        catch (Exception ex)
        {
            throw new DataPathUnavailableException(
                $"Der Datenpfad \"{dataPath}\" ist nicht erreichbar. Bitte prüfen Sie die Netzwerkverbindung " +
                "und den Pfad und versuchen Sie es erneut.", ex);
        }

        EnsureWritable(dataPath);

        try
        {
            Directory.CreateDirectory(Path.Combine(dataPath, "backups"));

            new PlayerXmlRepository(dataPath).EnsureFileExists();
            new MatchXmlRepository(dataPath).EnsureFileExists();
            new DoubleMatchXmlRepository(dataPath).EnsureFileExists();
            new AuditLogXmlRepository(dataPath).EnsureFileExists();
            new QuoteXmlRepository(dataPath).EnsureSeeded();
            new SeasonXmlRepository(dataPath).EnsureFileExists();
        }
        catch (Exception ex)
        {
            throw new DataPathUnavailableException(
                $"Am Datenpfad \"{dataPath}\" konnten die benötigten Dateien nicht angelegt werden: {ex.Message}", ex);
        }

        Logger.Info($"Datenpfad geprüft und bereit: {dataPath}");
    }

    private static void EnsureWritable(string dataPath)
    {
        var probeFile = Path.Combine(dataPath, $".write-check-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(probeFile, "ok");
        }
        catch (Exception ex)
        {
            throw new DataPathUnavailableException(
                $"Der Datenpfad \"{dataPath}\" ist nicht beschreibbar. Bitte prüfen Sie die Zugriffsrechte.", ex);
        }
        finally
        {
            try
            {
                if (File.Exists(probeFile)) File.Delete(probeFile);
            }
            catch
            {
                // Ignored - a leftover probe file is harmless.
            }
        }
    }
}
