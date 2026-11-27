using Domain.Pii;
using Infrastructure.Pii.Detectors;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

/// <summary>
/// Prompt 06 coverage: the three gaps carried from the PR 45 review, the
/// false-positive guards, and the card, date-of-birth and address types.
/// Every value is a fake from <see cref="FakePii"/>.
/// </summary>
public class PiiCoverageTests
{
    private static readonly PiiPolicy Policy = new(
        Convert.FromBase64String(FakePii.TestKeyBase64),
        [
            new AbaRoutingNumberDetector(),
            new CardNumberDetector(),
            new SsnDetector(),
            new EinItinDetector(),
            new DateOfBirthDetector(),
            new StreetAddressDetector(),
            new BankAccountNumberDetector(),
        ]);

    [Fact]
    public void Masks_an_unbroken_run_of_18_digits()
    {
        var masked = Policy.Mask($"Ref {FakePii.LongUnbrokenRun} end").MaskedText;

        Assert.DoesNotContain(FakePii.LongUnbrokenRun, masked);
        Assert.Contains("[ACCT ****5678 fp:", masked);
    }

    [Fact]
    public void Masks_account_groups_joined_to_an_SSN_by_underscore()
    {
        var masked = Policy.Mask($"ID {FakePii.SsnJoinedToAccount} end").MaskedText;

        Assert.DoesNotContain("1234_5678", masked);
        Assert.DoesNotContain(FakePii.SsnFormatted, masked);
        Assert.Contains("[SSN ****6789 fp:", masked);
        Assert.Contains("[ACCT ****9012 fp:", masked);
    }

    [Theory]
    [InlineData(FakePii.SplitByDoubleSpace)]
    [InlineData(FakePii.SplitByDoubleDash)]
    [InlineData(FakePii.SplitByDashPair)]
    public void Masks_digit_runs_split_by_double_space_or_dash_pairs(string run)
    {
        var masked = Policy.Mask($"Acct {run} end").MaskedText;

        Assert.DoesNotContain("1234", masked);
        Assert.Contains("[ACCT ****9012 fp:", masked);
    }

    [Fact]
    public void Does_not_mask_a_list_of_invoice_numbers()
    {
        Assert.Equal(FakePii.InvoiceList, Policy.Mask(FakePii.InvoiceList).MaskedText);
    }

    [Theory]
    [InlineData("Call 555-123-4567 today")]
    [InlineData("Paid 2024-11-25 and 11/25/2024")]
    [InlineData("Due 25/11/2024")]
    [InlineData("Range 2024-11-25 - 2024-12-01")]
    public void Leaves_phone_numbers_and_plain_dates_unmasked(string text)
    {
        Assert.Equal(text, Policy.Mask(text).MaskedText);
    }

    [Fact]
    public void Masks_a_Luhn_valid_card_number_as_CARD()
    {
        var result = Policy.Mask($"Card {FakePii.CardNumberSpaced} on file");

        Assert.Contains("[CARD ****1111 fp:", result.MaskedText);
        Assert.DoesNotContain("4111", result.MaskedText);
        Assert.Equal(PiiType.Card, result.Matches.Single().Type);
    }

    [Fact]
    public void A_card_shaped_run_that_fails_Luhn_is_not_a_card()
    {
        var result = Policy.Mask($"Card {FakePii.CardNumberBadCheck} on file");

        Assert.DoesNotContain(result.Matches, m => m.Type == PiiType.Card);
    }

    [Fact]
    public void Masks_a_date_of_birth_after_its_label_only()
    {
        var masked = Policy.Mask($"DOB {FakePii.DateOfBirth} on file").MaskedText;

        Assert.DoesNotContain(FakePii.DateOfBirth, masked);
        Assert.Contains("[DOB fp:", masked);
        Assert.Equal("Paid 04/12/1980", Policy.Mask("Paid 04/12/1980").MaskedText);
    }

    [Fact]
    public void Masks_a_street_address()
    {
        var masked = Policy.Mask($"Ship to {FakePii.StreetAddress}.").MaskedText;

        Assert.DoesNotContain(FakePii.StreetAddress, masked);
        Assert.Contains("[ADDR fp:", masked);
    }
}
