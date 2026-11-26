using Domain.Pii;

namespace Infrastructure.Options;

public class PiiOptions
{
    public const string SectionName = "Pii";

    /// <summary>
    /// Base64 local key for the fingerprint HMAC. When it is empty, the key
    /// comes from <see cref="FingerprintKeyPath"/>: generated once, then
    /// reused, so fingerprints are stable across restarts. The key never
    /// leaves the machine.
    /// </summary>
    public string? FingerprintKey { get; set; }

    /// <summary>Machine-local key file used when no explicit key is set.</summary>
    public string FingerprintKeyPath { get; set; } = "data/pii-fingerprint.key";
}
