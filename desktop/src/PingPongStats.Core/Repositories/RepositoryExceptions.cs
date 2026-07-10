namespace PingPongStats.Core.Repositories;

/// <summary>Thrown when an XML data file exists but cannot be parsed (corrupted/unreadable).</summary>
public class XmlDataCorruptException : Exception
{
    public string FilePath { get; }

    public XmlDataCorruptException(string filePath, string message, Exception? inner = null)
        : base(message, inner)
    {
        FilePath = filePath;
    }
}

/// <summary>Thrown when an exclusive write lock could not be acquired within the timeout
/// because another user/process is currently writing the same file.</summary>
public class DataFileLockTimeoutException : Exception
{
    public DataFileLockTimeoutException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when the configured data path is not reachable or not writable.</summary>
public class DataPathUnavailableException : Exception
{
    public DataPathUnavailableException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
