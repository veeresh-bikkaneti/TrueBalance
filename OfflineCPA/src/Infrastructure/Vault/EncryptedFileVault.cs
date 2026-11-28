using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Uploads;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

[assembly: InternalsVisibleTo("TrueBalance.Tests")]

namespace Infrastructure.Vault;

/// <summary>
/// The document vault. Each file is sealed with AES-256-GCM under a data key.
/// The settings file holds that key sealed by a key derived from the owner's
/// passphrase. The plain data key lives only in this object's memory, so a restart
/// locks the vault again.
/// </summary>
public sealed class EncryptedFileVault : IDocumentVault, IVaultControl
{
    private const string SettingsFile = "vault.json";
    private const string Sealed = ".enc";
    private const string Kdf = "PBKDF2-HMAC-SHA256";
    // Authenticated with the wrapped key. No stored document can have this name.
    private const string PassphraseWrap = SettingsFile + ":passphrase";
    private const string WrongPassphrase = "That passphrase does not open the books.";
    private const int Iterations = 600_000;
    private const int MinPassphraseLength = 12;
    private const int MaxOriginalNameLength = 200;

    private readonly UploadOptions _options;
    private readonly string _root;
    private readonly SemaphoreSlim _unlocking = new(1, 1);
    private byte[]? _key;

    public EncryptedFileVault(IOptions<UploadOptions> options)
    {
        _options = options.Value;
        _root = Path.GetFullPath(_options.Directory);
        Directory.CreateDirectory(_root);
    }

    private sealed record KeyWrap(byte[] Salt, byte[] SealedKey);

    // Version 1 has Salt and Check. Version 2 has Passphrase, and Recovery stays null for now.
    private sealed record VaultSettings(
        int Version, string Kdf, int Iterations,
        byte[]? Salt = null, byte[]? Check = null,
        KeyWrap? Passphrase = null, KeyWrap? Recovery = null);

    public bool IsUnlocked => _key is not null;

    public async Task<int> UnlockAsync(string passphrase, CancellationToken cancellationToken = default)
    {
        await _unlocking.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(_root, SettingsFile);
            var key = File.Exists(path)
                ? await OpenAndUpgradeAsync(path, passphrase, cancellationToken)
                : await CreateAsync(path, passphrase, cancellationToken);
            var encrypted = await EncryptExistingUploadsAsync(key, cancellationToken);
            _key = key;
            return encrypted;
        }
        finally
        {
            _unlocking.Release();
        }
    }

    public async Task ChangePassphraseAsync(string currentPassphrase, string newPassphrase, CancellationToken cancellationToken = default)
    {
        RequireLongEnough(newPassphrase);
        await _unlocking.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(_root, SettingsFile);
            if (!File.Exists(path))
            {
                throw new VaultException(WrongPassphrase);
            }

            var (settings, dataKey) = await OpenAsync(path, currentPassphrase, cancellationToken);
            await WriteSettingsAsync(path, Rewrap(settings, dataKey, newPassphrase), cancellationToken);
        }
        finally
        {
            _unlocking.Release();
        }
    }

    /// <summary>Puts the finished temporary settings file in place. A test swaps it to act out a crash.</summary>
    internal Action<string, string, bool> Replace { get; set; } = (from, to, overwrite) => File.Move(from, to, overwrite);

    public async Task<StoredUpload> SaveAsync(
        Stream content,
        long length,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var key = Key();
        if (length <= 0)
        {
            throw new InvalidUploadException("The uploaded file is empty.");
        }

        if (length > _options.MaxBytes)
        {
            throw TooLarge();
        }

        var safeOriginalName = SanitizeFileName(originalFileName);
        var extension = Path.GetExtension(safeOriginalName).ToLowerInvariant();

        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new InvalidUploadException($"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", _options.AllowedExtensions)}.");
        }

        if (!string.IsNullOrEmpty(contentType) && !_options.AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new InvalidUploadException($"Content type '{contentType}' is not allowed.");
        }

        // Read into memory so no plain copy reaches the disk. The declared length is not trusted.
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > _options.MaxBytes)
            {
                throw TooLarge();
            }

            buffer.Write(chunk, 0, read);
        }

        var id = Guid.NewGuid();
        var name = id.ToString("N") + extension;
        var plain = buffer.ToArray();
        var fullPath = await WriteSealedAsync(key, name, plain, cancellationToken);
        return new StoredUpload(id, name, safeOriginalName, extension, plain.Length, fullPath);
    }

    public async Task<Stream?> OpenReadAsync(Guid documentId, string extension, CancellationToken cancellationToken = default)
    {
        var key = Key();
        // Only allow-listed extensions, so a stored value cannot steer the path.
        if (!_options.AllowedExtensions.Contains(extension))
        {
            return null;
        }

        var name = documentId.ToString("N") + extension;
        var path = Path.Combine(_root, name + Sealed);
        return File.Exists(path)
            ? new MemoryStream(Open(key, name, await File.ReadAllBytesAsync(path, cancellationToken)), writable: false)
            : null;
    }

    public Task<int> PurgeOriginalsAsync(CancellationToken cancellationToken = default)
    {
        Key();
        var documents = Directory.GetFiles(_root).Where(f => IsDocument(Path.GetFileName(f))).ToList();
        documents.ForEach(File.Delete);
        return Task.FromResult(documents.Count);
    }

    internal static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\0', '_'));
        name = new string(name.Select(c => char.IsControl(c) ? '_' : c).ToArray());

        if (name.Length > MaxOriginalNameLength)
        {
            name = name[^MaxOriginalNameLength..];
        }

        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }

    private byte[] Key() => _key ?? throw new VaultLockedException();

    private InvalidUploadException TooLarge() =>
        new($"The uploaded file exceeds the {_options.MaxBytes / (1024 * 1024)} MB size limit.");

    /// <summary>True for a generated document name, sealed or not. The settings file never matches.</summary>
    private bool IsDocument(string fileName)
    {
        var name = fileName.EndsWith(Sealed, StringComparison.Ordinal) ? fileName[..^Sealed.Length] : fileName;
        return _options.AllowedExtensions.Contains(Path.GetExtension(name))
            && Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _);
    }

    private async Task<byte[]> CreateAsync(string path, string passphrase, CancellationToken ct)
    {
        if (Directory.EnumerateFiles(_root, "*" + Sealed).Any())
        {
            throw new VaultException("Encrypted documents exist but the vault settings file is missing. Restore it from a backup.");
        }

        RequireLongEnough(passphrase);
        var dataKey = RandomNumberGenerator.GetBytes(32);
        await WriteSettingsAsync(path, Rewrap(new VaultSettings(2, Kdf, Iterations), dataKey, passphrase), ct, overwrite: false);
        return dataKey;
    }

    /// <summary>
    /// A version 1 vault sealed its files with the passphrase key itself. That key
    /// becomes the data key, so no document is encrypted again.
    /// </summary>
    private async Task<byte[]> OpenAndUpgradeAsync(string path, string passphrase, CancellationToken ct)
    {
        var (settings, dataKey) = await OpenAsync(path, passphrase, ct);
        if (settings.Version == 1)
        {
            await WriteSettingsAsync(path, Rewrap(settings, dataKey, passphrase), ct);
        }

        return dataKey;
    }

    private static async Task<(VaultSettings Settings, byte[] DataKey)> OpenAsync(string path, string passphrase, CancellationToken ct)
    {
        const string damaged = "The vault settings file is damaged. Restore it from a backup.";
        VaultSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<VaultSettings>(await File.ReadAllTextAsync(path, ct));
        }
        catch (JsonException)
        {
            throw new VaultException(damaged);
        }

        var complete = settings is
            { Version: 1, Salt.Length: >= 16, Check: not null } or
            { Version: 2, Passphrase: { Salt.Length: >= 16, SealedKey: not null } };
        if (!complete || settings!.Kdf != Kdf || settings.Iterations < Iterations)
        {
            throw new VaultException(damaged);
        }

        var salt = settings.Version == 1 ? settings.Salt! : settings.Passphrase!.Salt;
        var derived = Derive(passphrase ?? string.Empty, salt, settings.Iterations);
        try
        {
            if (settings.Version == 2)
            {
                return (settings, Open(derived, PassphraseWrap, settings.Passphrase!.SealedKey));
            }

            Open(derived, SettingsFile, settings.Check!);
            return (settings, derived);
        }
        catch (VaultException)
        {
            throw new VaultException(WrongPassphrase);
        }
    }

    /// <summary>Version 2 settings with the data key sealed under the passphrase and a new salt.</summary>
    private static VaultSettings Rewrap(VaultSettings settings, byte[] dataKey, string passphrase)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var sealedKey = Seal(Derive(passphrase, salt, settings.Iterations), PassphraseWrap, dataKey);
        return settings with { Version = 2, Salt = null, Check = null, Passphrase = new KeyWrap(salt, sealedKey) };
    }

    /// <summary>Writes beside the old file, flushes to disk, then renames over it.</summary>
    private async Task WriteSettingsAsync(string path, VaultSettings settings, CancellationToken ct, bool overwrite = true)
    {
        var temporary = path + ".tmp";
        await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write))
        {
            await JsonSerializer.SerializeAsync(stream, settings, cancellationToken: ct);
            stream.Flush(flushToDisk: true);
        }

        Replace(temporary, path, overwrite);
    }

    private static void RequireLongEnough(string passphrase)
    {
        if (passphrase is null || passphrase.Length < MinPassphraseLength)
        {
            throw new VaultException($"Use a passphrase of at least {MinPassphraseLength} characters.");
        }
    }

    /// <summary>
    /// Seals each plain upload, proves it opens again, and only then deletes the
    /// plain copy. A second run finds nothing to do.
    /// </summary>
    private async Task<int> EncryptExistingUploadsAsync(byte[] key, CancellationToken ct)
    {
        var plainFiles = Directory.GetFiles(_root)
            .Where(f => !f.EndsWith(Sealed, StringComparison.Ordinal) && IsDocument(Path.GetFileName(f)))
            .ToList();
        foreach (var file in plainFiles)
        {
            var name = Path.GetFileName(file);
            var plain = await File.ReadAllBytesAsync(file, ct);
            var path = await WriteSealedAsync(key, name, plain, ct);
            if (!Open(key, name, await File.ReadAllBytesAsync(path, ct)).AsSpan().SequenceEqual(plain))
            {
                throw new VaultException("An encrypted file did not match its original. The original was kept.");
            }

            File.Delete(file);
        }

        return plainFiles.Count;
    }

    private async Task<string> WriteSealedAsync(byte[] key, string name, byte[] plain, CancellationToken ct)
    {
        var path = Path.Combine(_root, name + Sealed);
        // The temporary file holds ciphertext only.
        await File.WriteAllBytesAsync(path + ".tmp", Seal(key, name, plain), ct);
        File.Move(path + ".tmp", path, overwrite: true);
        return path;
    }

    private static byte[] Derive(string passphrase, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, iterations, HashAlgorithmName.SHA256, 32);

    // Layout: 12-byte nonce, 16-byte tag, ciphertext. The file name is authenticated,
    // so one sealed file cannot stand in for another.
    private static byte[] Seal(byte[] key, string name, byte[] plain)
    {
        var output = new byte[12 + 16 + plain.Length];
        RandomNumberGenerator.Fill(output.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(output.AsSpan(0, 12), plain, output.AsSpan(28), output.AsSpan(12, 16), Encoding.UTF8.GetBytes(name));
        return output;
    }

    private static byte[] Open(byte[] key, string name, byte[] data)
    {
        const string failed = "A stored document could not be decrypted.";
        if (data.Length < 28)
        {
            throw new VaultException(failed);
        }

        var plain = new byte[data.Length - 28];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(data.AsSpan(0, 12), data.AsSpan(28), data.AsSpan(12, 16), plain, Encoding.UTF8.GetBytes(name));
        }
        catch (CryptographicException)
        {
            throw new VaultException(failed);
        }

        return plain;
    }
}
