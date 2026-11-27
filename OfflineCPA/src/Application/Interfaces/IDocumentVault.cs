using Application.Uploads;

namespace Application.Interfaces;

/// <summary>
/// Port for the document vault: an adapter stores one uploaded file under a
/// generated name and never echoes the client's filename back out.
/// </summary>
public interface IDocumentVault
{
    Task<StoredUpload> SaveAsync(
        Stream content,
        long length,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken = default);

    /// <summary>False until the owner's passphrase opens the books.</summary>
    bool IsUnlocked { get; }

    /// <exception cref="VaultLockedException">The vault is locked.</exception>
    void EnsureUnlocked()
    {
        if (!IsUnlocked) throw new VaultLockedException();
    }

    /// <summary>Decrypted content held in memory, or null when the document is not there.</summary>
    Task<Stream?> OpenReadAsync(Guid documentId, string extension, CancellationToken cancellationToken = default);
}
