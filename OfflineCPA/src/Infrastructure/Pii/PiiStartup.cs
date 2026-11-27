using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Pii;

/// <summary>Startup notice for a new key. It lives outside the masker,
/// because the logger itself depends on the masker.</summary>
public static class PiiStartup
{
    public static void WarnIfKeyGenerated(IPiiMasker masker, ILogger logger)
    {
        if (masker is PiiMasker { GeneratedKeyFile: { } file })
        {
            logger.LogWarning(
                "Generated a new PII fingerprint key file ({KeyFile}). Back it up; losing it breaks fingerprint matching.",
                file);
        }
    }
}
