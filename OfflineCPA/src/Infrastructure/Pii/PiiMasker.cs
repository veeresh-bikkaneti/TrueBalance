using Application.Interfaces;
using Domain.Pii;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Pii;

/// <summary>
/// The adapter behind <see cref="IPiiMasker"/>: it owns the local key and
/// the detector set and delegates all decisions to the domain
/// <see cref="PiiPolicy"/>. The key comes from configuration when set;
/// otherwise it is generated once and kept in a machine-local file, so
/// fingerprints survive restarts.
/// </summary>
public sealed class PiiMasker : IPiiMasker
{
    private readonly PiiPolicy _policy;

    public PiiMasker(IOptions<PiiOptions> options, IEnumerable<IPiiDetector> detectors)
    {
        var settings = options.Value;
        byte[] key;
        if (string.IsNullOrWhiteSpace(settings.FingerprintKey))
        {
            key = LocalFingerprintKeyStore.LoadOrCreate(settings.FingerprintKeyPath);
        }
        else
        {
            try
            {
                key = Convert.FromBase64String(settings.FingerprintKey);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "Pii:FingerprintKey must be base64. Generate one with: openssl rand -base64 32");
            }

            if (key.Length < 16)
            {
                throw new InvalidOperationException("Pii:FingerprintKey must hold at least 16 bytes.");
            }
        }

        _policy = new PiiPolicy(key, detectors.ToList());
    }

    public string Mask(string text)
    {
        return _policy.Mask(text).MaskedText;
    }

    public PiiMaskingResult MaskDetailed(string text)
    {
        return _policy.Mask(text);
    }
}
