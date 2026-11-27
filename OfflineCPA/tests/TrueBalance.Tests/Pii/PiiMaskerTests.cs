using Infrastructure.Options;
using Infrastructure.Pii;
using Infrastructure.Pii.Detectors;
using Microsoft.Extensions.Options;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

public class PiiMaskerTests
{
    [Fact]
    public void Unset_key_is_generated_once_and_reused_from_the_local_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tb-fingerprint-{Guid.NewGuid():N}.key");
        try
        {
            var first = FingerprintOf(MaskWith(path, configured: null));
            var second = FingerprintOf(MaskWith(path, configured: null));

            // Two masker instances (two "restarts") must agree: the key is
            // generated once and persisted, never regenerated.
            Assert.Equal(first, second);
            Assert.True(new FileInfo(path).Length >= 16);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Configured_base64_key_takes_precedence_over_the_local_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tb-fingerprint-{Guid.NewGuid():N}.key");
        try
        {
            var fromFile = FingerprintOf(MaskWith(path, configured: null));
            var fromConfig = FingerprintOf(MaskWith(path, configured: FakePii.TestKeyBase64));

            Assert.NotEqual(fromFile, fromConfig);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Malformed_or_short_keys_are_rejected_at_startup()
    {
        Assert.Throws<InvalidOperationException>(() => MaskWith("unused.key", configured: "not-base64!!"));

        var shortKey = Convert.ToBase64String(new byte[8]);
        Assert.Throws<InvalidOperationException>(() => MaskWith("unused.key", configured: shortKey));
    }

    private static string FingerprintOf(PiiMasker masker)
    {
        return masker.MaskDetailed($"Acct {FakePii.AccountNumberA}").Matches[0].Fingerprint;
    }

    private static PiiMasker MaskWith(string path, string? configured)
    {
        var options = Options.Create(new PiiOptions
        {
            FingerprintKey = configured,
            FingerprintKeyPath = path,
        });

        return new PiiMasker(options,
        [
            new AbaRoutingNumberDetector(),
            new SsnDetector(),
            new EinItinDetector(),
            new BankAccountNumberDetector(),
        ]);
    }
}
