using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

public class AddressFingerprintTests
{
    private static string Fingerprint(string address)
        => Assert.Single(new MaskingPiiMasker().MaskDetailed(address).Matches).Fingerprint;

    [Fact]
    public void Different_addresses_with_the_same_house_number_get_different_fingerprints()
        => Assert.NotEqual(Fingerprint("742 Evergreen Terrace"), Fingerprint(FakePii.AddressHyphenName));

    [Fact]
    public void Case_spacing_and_punctuation_do_not_change_an_address_fingerprint()
        => Assert.Equal(Fingerprint("742 Evergreen Terrace"), Fingerprint("742  evergreen terrace."));
}
