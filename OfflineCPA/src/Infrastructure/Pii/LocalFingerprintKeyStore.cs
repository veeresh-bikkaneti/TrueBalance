using System.Security.Cryptography;

namespace Infrastructure.Pii;

/// <summary>
/// Loads the local fingerprint key from a machine-local file, generating
/// and persisting a new key on first start, so fingerprints stay stable
/// across restarts even when Pii:FingerprintKey is not configured. The
/// file must stay outside version control and never leave the machine.
/// </summary>
public static class LocalFingerprintKeyStore
{
    private const int KeyBytes = 32;
    private const int MinKeyBytes = 16;

    public static byte[] LoadOrCreate(string path)
    {
        var fullPath = Path.GetFullPath(path);
        try
        {
            var existing = File.ReadAllBytes(fullPath);
            if (existing.Length >= MinKeyBytes)
            {
                return existing;
            }
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }

        var key = RandomNumberGenerator.GetBytes(KeyBytes);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            File.WriteAllBytes(fullPath, key);
        }
        catch (IOException)
        {
            // Another process or thread created the key first; use theirs.
            var existing = File.ReadAllBytes(fullPath);
            if (existing.Length >= MinKeyBytes)
            {
                return existing;
            }
        }

        return key;
    }
}
