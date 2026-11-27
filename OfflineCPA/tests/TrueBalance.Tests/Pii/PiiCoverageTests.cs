using System.Text.RegularExpressions;
using Domain.Pii;
using Infrastructure.Pii;
using Infrastructure.Pii.Detectors;
using Microsoft.Extensions.DependencyInjection;
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

    [Theory]
    [InlineData("Acct 1234 5678 9012 3456 78")]
    [InlineData("Wire to 9876 5432 1098 7654 3210")]
    [InlineData("Acct 123 456 789 012 345 678")]
    [InlineData("1234 5678 9012 3456 7890 1234")]
    [InlineData("Paid 1001 1002 1003 1004 1005")]
    [InlineData("Reorder 1001 1002 1003 1004 1005")]
    [InlineData("Invoice total 1001 1002 1003 1004 1005")]
    [InlineData("Invoices 12345678 90123456 78901234")]
    public void Masks_space_grouped_numbers_without_a_list_label(string text)
    {
        var masked = Policy.Mask(text).MaskedText;

        var outsideTokens = Regex.Replace(masked, @"\[[^\]]*\]", string.Empty);
        Assert.DoesNotMatch("[0-9]", outsideTokens);
    }

    [Theory]
    [InlineData("invoice 1001 1002 1003 1004 1005")]
    [InlineData("INV 1001 1002 1003 1004 1005")]
    [InlineData("Inv # 1001 1002 1003 1004 1005")]
    [InlineData("Check 1001 1002 1003")]
    [InlineData("Checks 1001 1002 1003 1004 1005")]
    [InlineData("Chk 100123 100124 100125 100126")]
    [InlineData("Order 1001 1002 1003 1004 1005")]
    [InlineData("Orders 1001 1002 1003 1004 1005")]
    public void Leaves_a_labeled_number_list_readable(string text)
    {
        Assert.Equal(text, Policy.Mask(text).MaskedText);
    }

    [Theory]
    [InlineData("born 04/12/1980")]
    [InlineData("Birth date 1980-04-12")]
    [InlineData("birthdate: 04/12/1980")]
    [InlineData("Date of birth 04/12/1980")]
    public void Masks_a_date_after_any_birth_label(string text)
    {
        var masked = Policy.Mask(text).MaskedText;

        Assert.Contains("[DOB fp:", masked);
        Assert.DoesNotContain("1980", masked);
    }

    [Fact]
    public void Leaves_a_statement_date_readable()
    {
        const string text = "Statement date 04/12/2026";

        Assert.Equal(text, Policy.Mask(text).MaskedText);
    }

    [Theory]
    [InlineData("742 Evergreen Terrace")]
    [InlineData("PO Box 1234")]
    [InlineData("P.O. Box 1234")]
    [InlineData("742 Example Street, Apt 4")]
    [InlineData("742 Example St. Suite 4B")]
    [InlineData("742 Example Road Ste 12")]
    [InlineData("742 Example Lane, Unit B")]
    [InlineData("742 Example Drive #4")]
    public void Masks_the_whole_address_including_the_unit(string address)
    {
        var masked = Policy.Mask($"Ship to {address} today").MaskedText;

        Assert.Matches(@"^Ship to \[ADDR fp:[0-9a-f]+\] today$", masked);
    }

    [Fact]
    public void App_and_test_maskers_register_the_same_detectors()
    {
        var services = new ServiceCollection();
        new PiiModule().Register(services);
        using var provider = services.BuildServiceProvider();

        var app = provider.GetServices<IPiiDetector>().Select(d => d.GetType().Name).Order();
        var test = new MaskingPiiMasker().Detectors.Select(d => d.GetType().Name).Order();

        Assert.Equal(app, test);
    }
}
