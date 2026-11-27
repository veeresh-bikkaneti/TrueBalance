namespace Application.Interfaces;

/// <summary>The owner's actions on the vault, apart from storing and reading documents.</summary>
public interface IVaultControl
{
    /// <summary>Opens the vault, creating it on first use, and seals any plain upload left behind.</summary>
    /// <returns>The number of plain files that were encrypted.</returns>
    Task<int> UnlockAsync(string passphrase, CancellationToken cancellationToken = default);

    /// <summary>Deletes stored documents. Ledger entries and staged rows are untouched.</summary>
    /// <returns>The number of files deleted.</returns>
    Task<int> PurgeOriginalsAsync(CancellationToken cancellationToken = default);
}
