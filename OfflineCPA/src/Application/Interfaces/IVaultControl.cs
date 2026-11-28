namespace Application.Interfaces;

/// <summary>The owner's actions on the vault, apart from storing and reading documents.</summary>
public interface IVaultControl
{
    /// <summary>Opens the vault, creating it on first use, and seals any plain upload left behind.</summary>
    /// <returns>The number of plain files that were encrypted.</returns>
    Task<int> UnlockAsync(string passphrase, CancellationToken cancellationToken = default);

    /// <summary>Rewraps the data key under a new passphrase. No stored document changes.</summary>
    Task ChangePassphraseAsync(string currentPassphrase, string newPassphrase, CancellationToken cancellationToken = default);

    /// <summary>Makes a recovery code for the unlocked vault, given its passphrase. The code made before stops working.</summary>
    /// <returns>The code. It is not stored, so it cannot be read again.</returns>
    Task<string> CreateRecoveryCodeAsync(string passphrase, CancellationToken cancellationToken = default);

    /// <summary>Sets a new passphrase with the recovery code, which then stops working, and opens the vault.</summary>
    /// <returns>The number of plain files that were encrypted.</returns>
    Task<int> RecoverAsync(string recoveryCode, string newPassphrase, CancellationToken cancellationToken = default);

    /// <summary>Deletes stored documents. Ledger entries and staged rows are untouched.</summary>
    /// <returns>The number of files deleted.</returns>
    Task<int> PurgeOriginalsAsync(CancellationToken cancellationToken = default);
}
