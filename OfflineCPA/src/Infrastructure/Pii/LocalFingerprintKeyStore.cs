using System.Security.Cryptography;

namespace Infrastructure.Pii;

/// <summary>
/// Loads the local fingerprint key from a machine-local file, creating it
/// only when no file exists, so fingerprints stay stable across restarts.
/// It fails closed: a file that is too short or unreadable stops startup and
/// is never overwritten. The file must stay outside version control, belongs
/// in backups, and never leaves the machine.
/// </summary>
public static class LocalFingerprintKeyStore
{
    private const int KeyBytes = 32;
    private const int MinKeyBytes = 16;
    private const int ReadAttempts = 20;
    private static readonly TimeSpan ReadDelay = TimeSpan.FromMilliseconds(50);

    public static byte[] LoadOrCreate(string path) => LoadOrCreate(path, out _);

    public static byte[] LoadOrCreate(string path, out bool generated)
    {
        generated = false;
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            return Read(fullPath);
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        var key = RandomNumberGenerator.GetBytes(KeyBytes);
        try
        {
            using var stream = new FileStream(fullPath, options);
            stream.Write(key);
        }
        catch (IOException) when (File.Exists(fullPath))
        {
            // Another process or thread created the key first; use theirs.
            return Read(fullPath);
        }

        generated = true;
        return key;
    }

    /// <summary>Retries briefly, because a process that just created the
    /// file may still be writing it.</summary>
    private static byte[] Read(string fullPath)
    {
        Exception? failure = null;
        for (var attempt = 0; attempt < ReadAttempts; attempt++)
        {
            try
            {
                var key = File.ReadAllBytes(fullPath);
                if (key.Length >= MinKeyBytes)
                {
                    return key;
                }

                failure = null;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                failure = e;
            }

            Thread.Sleep(ReadDelay);
        }

        throw new InvalidOperationException(
            $"The PII fingerprint key file '{fullPath}' is unreadable or shorter than {MinKeyBytes} bytes. " +
            "Restore the key from a backup, or delete the file on purpose to start a new one " +
            "(earlier fingerprints will no longer match).",
            failure);
    }
}
