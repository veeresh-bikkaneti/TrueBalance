using Domain.Pii;
using Infrastructure.Pii;
using Infrastructure.Pii.Detectors;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

public class PiiDetectorTests
{
    [Fact]
    public void Detects_formatted_and_bare_SSNs()
    {
        var detector = new SsnDetector();
        var formatted = detector.Find($"employee {FakePii.SsnFormatted} paid").Single();
        var bare = detector.Find($"employee {FakePii.SsnBare} paid").Single();

        Assert.Equal(PiiType.Ssn, formatted.Type);
        Assert.Equal(FakePii.SsnFormatted, formatted.RawValue);
        Assert.Equal(PiiType.Ssn, bare.Type);
    }

    [Fact]
    public void Skips_invalid_SSN_ranges()
    {
        var detector = new SsnDetector();

        Assert.Empty(detector.Find("SSN 000-45-6789"));
        Assert.Empty(detector.Find("SSN 666-45-6789"));
        Assert.Empty(detector.Find("SSN 123-00-6789"));
        Assert.Empty(detector.Find("SSN 123-45-0000"));
    }

    [Fact]
    public void Detects_formatted_EIN_and_ITIN()
    {
        var detector = new EinItinDetector();

        var ein = detector.Find($"EIN {FakePii.EinFormatted} on file").Single();
        Assert.Equal(PiiType.EinItin, ein.Type);
        Assert.Equal(FakePii.EinFormatted, ein.RawValue);

        var itin = detector.Find($"ITIN {FakePii.ItinFormatted} on file").Single();
        Assert.Equal(PiiType.EinItin, itin.Type);
        Assert.Equal(FakePii.ItinFormatted, itin.RawValue);
    }

    [Fact]
    public void Skips_NNN_NN_NNNN_numbers_outside_ITIN_ranges()
    {
        var detector = new EinItinDetector();

        Assert.Empty(detector.Find("Ref 900-12-3456")); // middle digits 12: not an ITIN range
    }

    [Fact]
    public void Detects_checksum_valid_routing_numbers_only()
    {
        var detector = new AbaRoutingNumberDetector();
        var found = detector.Find($"ABA {FakePii.RoutingNumber} on check").Single();

        Assert.Equal(PiiType.AbaRoutingNumber, found.Type);
        Assert.Equal(FakePii.RoutingNumber, found.RawValue);
        Assert.Empty(detector.Find("ABA 123456789 on check"));
    }

    [Fact]
    public void Detects_bank_account_digit_runs()
    {
        var detector = new BankAccountNumberDetector();
        var found = detector.Find($"Acct {FakePii.AccountNumberA} debit").Single();

        Assert.Equal(PiiType.BankAccountNumber, found.Type);
        Assert.Equal(FakePii.AccountNumberA, found.RawValue);
    }

    [Theory]
    [InlineData("Invoice INV-0012345 dated today", "seven-digit invoice number")]
    [InlineData("Call 555-123-4567 for details", "US phone number")]
    [InlineData("Total $1,234.56 due", "decimal amount")]
    [InlineData("Balance $12345678.90 carried", "large amount with cents")]
    [InlineData("Paid on 2024-11-25", "dashed date")]
    [InlineData("Paid on 20241125", "bare date")]
    [InlineData("Reference 12345", "short reference")]
    [InlineData("Ref 900-12-3456 filed", "SSN-shaped number that failed SSN/ITIN validation")]
    public void Does_not_flag_common_false_positives(string text, string _)
    {
        var detector = new BankAccountNumberDetector();

        Assert.Empty(detector.Find(text));
    }
}
