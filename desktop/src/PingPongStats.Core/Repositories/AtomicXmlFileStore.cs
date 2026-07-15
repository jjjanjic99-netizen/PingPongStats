using PingPongStats.Core.Helpers;
using System.IO;

namespace PingPongStats.Core.Repositories;

/// <summary>
/// Generic atomic read/write store for a list of items persisted as a single
/// XML file. Implements the required write sequence: acquire exclusive lock,
/// read current file (fresh, inside the lock), back it up, write a temp file,
/// validate the temp file by re-reading it, then atomically replace the
/// original. Reads (<see cref="GetAll"/>) do not take the lock, so concurrent
/// readers are never blocked by a reader or by an in-progress write (which
/// only touches ".tmp" until the final replace).
/// </summary>
public sealed class AtomicXmlFileStore<T>
{
    private readonly string _filePath;
    private readonly string _backupDirectory;
    private readonly string _backupPrefix;
    private readonly Func<Stream, List<T>> _deserialize;
    private readonly Action<Stream, List<T>> _serialize;
    private readonly TimeSpan _lockTimeout;

    public AtomicXmlFileStore(
        string filePath,
        string backupDirectory,
        string backupPrefix,
        Func<Stream, List<T>> deserialize,
        Action<Stream, List<T>> serialize,
        TimeSpan? lockTimeout = null)
    {
        _filePath = filePath;
        _backupDirectory = backupDirectory;
        _backupPrefix = backupPrefix;
        _deserialize = deserialize;
        _serialize = serialize;
        _lockTimeout = lockTimeout ?? TimeSpan.FromSeconds(10);
    }

    public string FilePath => _filePath;

    /// <summary>Reads the current list. Concurrent reads are always allowed.
    /// Returns an empty list if the file does not exist yet.</summary>
    public List<T> GetAll()
    {
        if (!File.Exists(_filePath)) return new List<T>();

        try
        {
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return _deserialize(stream);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Xml.XmlException or IOException)
        {
            throw new XmlDataCorruptException(
                _filePath,
                $"Die Datei \"{Path.GetFileName(_filePath)}\" ist beschädigt oder nicht lesbar: {ex.Message}",
                ex);
        }
    }

    /// <summary>Creates an empty, valid XML file if it does not exist yet. Used during
    /// first-run bootstrap so the data folder is fully set up before normal use.</summary>
    public void EnsureFileExists()
    {
        if (File.Exists(_filePath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        WriteSerialized(_filePath, new List<T>());
    }

    /// <summary>
    /// Performs a full read-modify-write cycle under an exclusive lock so
    /// concurrent writers from other processes/users can never race:
    /// lock -&gt; read fresh -&gt; backup -&gt; write temp -&gt; validate temp -&gt; replace.
    /// </summary>
    public void Update(Func<List<T>, List<T>> mutate)
    {
        using var fileLock = FileLockHelper.AcquireExclusiveLock(_filePath, _lockTimeout);

        var current = GetAll();
        BackupIfExists();

        var updated = mutate(current) ?? new List<T>();

        var tempPath = _filePath + ".tmp";
        WriteSerialized(tempPath, updated);
        ValidateByReloading(tempPath);
        ReplaceOriginal(tempPath);
    }

    private void BackupIfExists()
    {
        if (!File.Exists(_filePath)) return;

        try
        {
            Directory.CreateDirectory(_backupDirectory);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(_backupDirectory, $"{_backupPrefix}_{timestamp}.xml");
            File.Copy(_filePath, backupPath, overwrite: true);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Backup von \"{_filePath}\" konnte nicht erstellt werden: {ex.Message}");
        }
    }

    private void WriteSerialized(string path, List<T> items)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        _serialize(stream, items);
    }

    private void ValidateByReloading(string tempPath)
    {
        try
        {
            using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _deserialize(stream);
        }
        catch (Exception ex)
        {
            TryDelete(tempPath);
            throw new XmlDataCorruptException(
                _filePath,
                $"Die neu geschriebene Datei \"{Path.GetFileName(_filePath)}\" konnte nicht validiert werden. " +
                "Die Änderung wurde verworfen, die ursprüngliche Datei blieb unverändert.",
                ex);
        }
    }

    private void ReplaceOriginal(string tempPath)
    {
        if (File.Exists(_filePath))
        {
            try
            {
                File.Replace(tempPath, _filePath, null);
            }
            catch (PlatformNotSupportedException)
            {
                // Some network filesystems don't support File.Replace; fall back to delete+move.
                File.Delete(_filePath);
                File.Move(tempPath, _filePath);
            }
        }
        else
        {
            File.Move(tempPath, _filePath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Ignored - best effort cleanup of a temp file.
        }
    }
}
