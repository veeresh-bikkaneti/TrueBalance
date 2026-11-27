using System.Text.RegularExpressions;
using Domain.Pii;
using Infrastructure.Pii.Detectors;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

/// <summary>
/// Prompt 06c: the values that still came out raw after the PR 51 coverage
/// work. Every value is fake.
/// </summary>
public class PiiGapTests
{
    private static readonly PiiPolicy Policy = new(
        Convert.FromBase64String(FakePii.TestKeyBase64), PiiDetectors.All());

    private static string Mask(string text) => Policy.Mask(text).MaskedText;

    private static string OutsideTokens(string masked) => Regex.Replace(masked, @"\[[^\]]*\]", string.Empty);

    [Theory]
    [InlineData("Pay by check " + FakePii.LabeledFourByFour)]
    [InlineData("Invoice " + FakePii.LabeledFourByFour)]
    [InlineData("Order " + FakePii.LabeledFourSixFive)]
    [InlineData("Check " + FakePii.LabeledFourSixFour)]
    [InlineData("Checks 1001 1002 1003 1004")]
    public void Masks_a_card_shaped_run_after_a_list_label(string text)
    {
        Assert.DoesNotMatch("[0-9]", OutsideTokens(Mask(text)));
    }

    [Theory]
    [InlineData("Invoices 1001 1002 1003 1004 1005")]
    [InlineData("Check 1001")]
    [InlineData("Statement date 04/12/1980")]
    [InlineData("Posted April 12, 1980")]
    [InlineData("Total $402.55")]
    [InlineData("Paid 4025550.182 and 402,555.01")]
    public void Leaves_lists_dates_and_amounts_readable(string text)
    {
        Assert.Equal(text, Mask(text));
    }

    [Theory]
    [InlineData("DOB ", FakePii.DobTwoDigitYear)]
    [InlineData("DOB - ", FakePii.DateOfBirth)]
    [InlineData("DOB: ", FakePii.DobMonthName)]
    [InlineData("DOB ", FakePii.DobDayMonthName)]
    [InlineData("Born on ", "Apr. 12 80")]
    [InlineData("Date of birth : ", "12-April-1980")]
    public void Masks_more_date_of_birth_shapes(string label, string date)
    {
        Assert.Matches($@"^{Regex.Escape(label)}\[DOB fp:[0-9a-f]+\] end$", Mask($"{label}{date} end"));
    }

    [Theory]
    [InlineData(FakePii.AddressHyphenName)]
    [InlineData(FakePii.AddressApostropheName)]
    [InlineData(FakePii.AddressValley)]
    [InlineData("12 Green Vly")]
    [InlineData("12 Example Bluffs")]
    [InlineData(FakePii.AddressFiveWords)]
    [InlineData("100 Example Sr Way")]
    [InlineData(FakePii.AddressOrdinal)]
    [InlineData(FakePii.AddressRuralRoute)]
    [InlineData(FakePii.AddressShortRural)]
    [InlineData(FakePii.AddressContractRoute)]
    [InlineData(FakePii.PoBoxWithUnit)]
    public void Masks_the_whole_address(string address)
    {
        Assert.Matches(@"^Ship to \[ADDR fp:[0-9a-f]+\] today$", Mask($"Ship to {address} today"));
    }

    [Theory]
    [InlineData("Paid 12 invoices this is late")]
    [InlineData("Paid 50 to John via Zelle")]
    [InlineData("ZELLE PAYMENT 50 TO JOHN VIA APP")]
    [InlineData("Bought 2 bottles of vitamin C")]
    [InlineData("Paid 3 invoices for the club")]
    [InlineData("Refund 25 for order at the mall")]
    [InlineData("Coffee 4 cups at the mill")]
    [InlineData("Lunch 3 people at Pine Key")]
    [InlineData("Uber 2 trips to the airport way")]
    [InlineData("Bought 10 tickets for the course")]
    [InlineData("Transfer 500 from savings via online banking")]
    [InlineData("Paid 1200 rent for the park space")]
    [InlineData("Paid 100 for the union dues")]
    [InlineData("Paid 50 a court fee")]
    [InlineData("Gas 40 a gallon")]
    public void Leaves_ordinary_memos_readable(string memo)
    {
        Assert.Equal(memo, Mask(memo));
    }

    [Theory]
    [InlineData("742 Evergreen Terrace", "")]
    [InlineData("742 Jean-Luc Road", "")]
    [InlineData("12 Green Valley", "")]
    [InlineData("100 Martin Luther King Jr Blvd", "")]
    [InlineData("1600 PENNSYLVANIA AVE", " NW")]
    [InlineData("Rural Route 2 Box 15", "")]
    [InlineData("PO Box 12 Apt 4", "")]
    [InlineData("100 A Street", "")]
    [InlineData("1234 A St", "")]
    [InlineData("742 Example Street, Apt 4", "")]
    public void Still_masks_a_street_line(string address, string readableTail)
    {
        Assert.Matches(
            $@"^Ship to \[ADDR fp:[0-9a-f]+\]{readableTail} today$",
            Mask($"Ship to {address}{readableTail} today"));
    }

    [Theory]
    [InlineData(FakePii.Phone)]
    [InlineData(FakePii.PhoneBracketed)]
    [InlineData("402.555.0182")]
    [InlineData("402 555 0182")]
    [InlineData("(402)555-0182")]
    [InlineData("+1 402-555-0182")]
    [InlineData("1-402-555-0182")]
    [InlineData("+14025550182")]
    [InlineData("402-555-0182 ext. 12")]
    [InlineData("(402) 555-0182 x345")]
    public void Masks_a_US_phone_number_with_no_readable_digits(string phone)
    {
        Assert.Matches(@"^Call \[PHONE fp:[0-9a-f]+\] today$", Mask($"Call {phone} today"));
    }

    [Theory]
    [InlineData("Ref 1234 402 555 0182")]
    [InlineData("Ref 1234  402 555 0182")]
    [InlineData("Ref 402 555 0182 9912")]
    [InlineData("Acct 99-402-555-0182")]
    [InlineData("Ref 1234 402-555-0182")]
    [InlineData("Acct 1234 5678 9012 Ref Way")]
    public void A_phone_shape_inside_a_longer_number_stays_fully_masked(string text)
    {
        Assert.DoesNotMatch("[0-9]", OutsideTokens(Mask(text)));
    }

    [Theory]
    [InlineData(FakePii.Email)]
    [InlineData("jane_doe+tax@mail.example.co.uk")]
    [InlineData("123456789@example.com")]
    [InlineData("jane.4025550182@example.com")]
    public void Masks_an_email_address_with_no_readable_part(string email)
    {
        Assert.Matches(@"^From \[EMAIL fp:[0-9a-f]+\]\. Done$", Mask($"From {email}. Done"));
    }

    [Fact]
    public void Email_fingerprints_differ_by_address_and_ignore_case()
    {
        static string Print(string email) => Policy.Mask(email).Matches.Single().Fingerprint;

        Assert.NotEqual(Print("jane@example.com"), Print("john@example.com"));
        Assert.Equal(Print("jane@example.com"), Print("Jane@Example.com"));
    }

    [Fact]
    public void Masks_a_phone_and_an_email_inside_a_statement_memo()
    {
        var masked = Mask($"Memo: refund $402.55, call {FakePii.Phone} or {FakePii.Email}");

        Assert.DoesNotContain(FakePii.Phone, masked);
        Assert.DoesNotContain(FakePii.Email, masked);
        Assert.Contains("$402.55", masked);
    }
}
