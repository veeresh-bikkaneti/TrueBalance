using Application.Interfaces;

namespace Application.Uploads;

public sealed record PurgeResult(bool Purged, int Deleted, string Warning);

/// <summary>Deletes the stored originals, and only when the owner confirms.</summary>
public sealed class PurgeOriginals(IVaultControl vault)
{
    public const string Warning =
        "Keep tax records for as long as the IRS says they may be needed: often 3 years, " +
        "and 6 or 7 years or indefinitely in some cases; employment tax records at least 4 years. " +
        "See https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records " +
        "[Moderate 1/1] and ask your CPA. Purged files cannot be recovered.";

    public async Task<PurgeResult> HandleAsync(bool confirm, CancellationToken cancellationToken = default) =>
        confirm
            ? new PurgeResult(true, await vault.PurgeOriginalsAsync(cancellationToken), Warning)
            : new PurgeResult(false, 0, Warning);
}
