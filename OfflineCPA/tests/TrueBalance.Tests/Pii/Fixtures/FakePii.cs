namespace TrueBalance.Tests.Pii.Fixtures;

/// <summary>
/// Synthetic, obviously fake PII values shared by every masking test. Prompt
/// 06 extends this folder instead of copying fixtures. None of these numbers
/// identifies a real person: the SSN is the classic placeholder, the EIN is
/// the IRS sample format, the ITIN middle digits use the documented IRS
/// range, the routing number was constructed to pass the check digit, and
/// the account numbers are arbitrary digit runs.
/// </summary>
public static class FakePii
{
    /// <summary>Shared synthetic HMAC key for every masking test.</summary>
    public const string TestKeyBase64 = "c3ludGhldGljLWtleS1mb3ItbWFza2luZy10ZXN0cy1vbmx5";

    public const string SsnFormatted = "123-45-6789";
    public const string SsnBare = "123456789";
    public const string EinFormatted = "12-3456789";
    public const string ItinFormatted = "900-70-1234";
    public const string RoutingNumber = "123456780";
    public const string AccountNumberA = "004567890123";
    public const string AccountNumberB = "998877660123";

    public const string SsnMixedSeparators = "123-45 6789";
    public const string ItinMixedSeparators = "912-70 1234";
    public const string LongRunUnderscoreFile = "acct_1234_5678_9012_2024_11_25.pdf";
    public const string LongRunHyphenFile = "acct-1234-5678-9012-2024-11-25.pdf";
    public const string LongRunAroundRouting = "123456780 1234 5678 9012";
    public const string LongUnbrokenRun = "100456789012345678";
    public const string SsnJoinedToAccount = "123-45-6789_1234_5678_9012";
    public const string SplitByDoubleSpace = "1234  5678  9012";
    public const string SplitByDoubleDash = "1234--5678--9012";
    public const string SplitByDashPair = "1234 - 5678 - 9012";
    public const string InvoiceList = "Invoices 1001 1002 1003 1004 1005";
    public const string CardNumberSpaced = "4111 1111 1111 1111";
    public const string CardNumberBadCheck = "4111 1111 1111 1112";
    public const string DateOfBirth = "04/12/1980";
    public const string StreetAddress = "742 Example Street";
    public const string DateDayFirstSlash = "25/11/2024";
    public const string DateDayFirstDot = "25.11.2024";
    public const string DotGroupedAmount = "12.345.678";
    public const string VersionPrefixedAccount = "v.1.1234.5678.9012";

    public const string Phone = "402-555-0182";
    public const string PhoneBracketed = "(402) 555-0182";
    public const string Email = "jane.doe@example.com";

    // Prompt 06c: card-shaped runs that fail the card check.
    public const string LabeledFourByFour = "1234 5678 9012 3456";
    public const string LabeledFourSixFive = "1234 567890 12345";
    public const string LabeledFourSixFour = "1234 567890 1234";

    public const string DobTwoDigitYear = "04/12/80";
    public const string DobMonthName = "April 12, 1980";
    public const string DobDayMonthName = "12 Apr 1980";

    public const string AddressHyphenName = "742 Jean-Luc Road";
    public const string AddressApostropheName = "742 O'Brien Street";
    public const string AddressValley = "12 Green Valley";
    public const string AddressFiveWords = "100 Martin Luther King Jr Blvd";
    public const string AddressOrdinal = "100 5th Avenue";
    public const string AddressRuralRoute = "Rural Route 2 Box 15";
    public const string AddressShortRural = "RR 2 Box 152";
    public const string AddressContractRoute = "HC 68 Box 23A";
    public const string PoBoxWithUnit = "PO Box 12 Apt 4";

    public static readonly string[] AllRaw =
    [
        SsnFormatted,
        SsnBare,
        EinFormatted,
        ItinFormatted,
        RoutingNumber,
        AccountNumberA,
        AccountNumberB,
        CardNumberSpaced,
        DateOfBirth,
        StreetAddress,
        Phone,
        Email,
    ];

    /// <summary>Raw digits of each value, for fingerprint comparisons.</summary>
    public static readonly string[] AllRawDigits = AllRaw
        .Select(v => new string(v.Where(char.IsDigit).ToArray()))
        .ToArray();

    /// <summary>The same fake value written with another script's digits.</summary>
    public static string InDigits(string value, char zero) =>
        new(value.Select(c => char.IsAsciiDigit(c) ? (char)(zero + (c - '0')) : c).ToArray());

    /// <summary>A synthetic statement body carrying every fake value.</summary>
    public static string SampleStatementText() =>
        $"Account {AccountNumberA} at routing {RoutingNumber}. " +
        $"Payroll for employee SSN {SsnFormatted} filed under EIN {EinFormatted}. " +
        $"Contractor ITIN {ItinFormatted}. Card {CardNumberSpaced}. " +
        $"DOB {DateOfBirth}. Ship to {StreetAddress}. Call {Phone} or write {Email}. Amount 12.50 on 2024-11-25 to ACME Corp.";

    /// <summary>The values that must never appear anywhere after masking.</summary>
    /// A date's bare digits are left out: the app treats them as a plain date.
    public static IEnumerable<string> LeakTargets() => AllRaw.Concat(
        AllRawDigits.Where(d => d.Length >= 8 && d != DateOfBirth.Replace("/", string.Empty)));
}
