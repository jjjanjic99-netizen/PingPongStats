using System.Text;
using System.IO;

namespace PingPongStats.Core.Helpers;

public enum LogLevel
{
    Info,
    Warn,
    Error,
}

/// <summary>
/// Minimal thread-safe file logger. Writes to a local per-user folder
/// (never to the shared network DataPath) so logging keeps working even if
/// the network share is unreachable.
/// </summary>
public static class Logger
{
    private static readonly object WriteLock = new();
    private static string? _logFilePath;

    public static void Initialize(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
    }

    public static void Info(string message) => Write(LogLevel.Info, message);

    public static void Warn(string message) => Write(LogLevel.Warn, message);

    public static void Error(string message, Exception? ex = null) =>
        Write(LogLevel.Error, ex is null ? message : $"{message} | {ex}");

    private static void Write(LogLevel level, string message)
    {
        if (_logFilePath is null) return;

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
        lock (WriteLock)
        {
            try
            {
                File.AppendAllText(_logFilePath, line, new UTF8Encoding(false));
            }
            catch
            {
                // Logging must never crash the application.
            }
        }
    }
}
