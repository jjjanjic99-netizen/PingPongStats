using System.IO;

namespace PingPongStats.Core.Repositories;

/// <summary>
/// Coordinates exclusive write access to a shared data file across multiple
/// processes/users on a network share, using a companion "*.lock" marker
/// file. FileStream(FileMode.CreateNew, FileShare.None) is atomic even on
/// SMB shares: only one process can win the race to create it.
/// </summary>
public static class FileLockHelper
{
    private static readonly TimeSpan StaleLockAge = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(300);

    public static IDisposable AcquireExclusiveLock(string targetFilePath, TimeSpan timeout)
    {
        var lockPath = targetFilePath + ".lock";
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            RemoveStaleLockIfAny(lockPath);

            try
            {
                var stream = new FileStream(lockPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                return new LockHandle(stream, lockPath);
            }
            catch (IOException)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    var fileName = Path.GetFileName(targetFilePath);
                    throw new DataFileLockTimeoutException(
                        $"Die Datei \"{fileName}\" wird gerade von einem anderen Benutzer bearbeitet. " +
                        "Bitte versuchen Sie es in Kürze erneut.");
                }

                Thread.Sleep(RetryDelay);
            }
        }
    }

    private static void RemoveStaleLockIfAny(string lockPath)
    {
        try
        {
            if (!File.Exists(lockPath)) return;
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(lockPath);
            if (age > StaleLockAge)
            {
                File.Delete(lockPath);
            }
        }
        catch
        {
            // Best-effort cleanup; if another process removes/recreates it concurrently
            // the CreateNew attempt below will simply retry.
        }
    }

    private sealed class LockHandle : IDisposable
    {
        private readonly FileStream _stream;
        private readonly string _lockPath;
        private bool _disposed;

        public LockHandle(FileStream stream, string lockPath)
        {
            _stream = stream;
            _lockPath = lockPath;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stream.Dispose();
            try
            {
                File.Delete(_lockPath);
            }
            catch
            {
                // Not fatal - a stale lock will be cleaned up by the next attempt.
            }
        }
    }
}
