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

    public static readonly string[] AllRaw =
    [
        SsnFormatted,
        SsnBare,
        EinFormatted,
        ItinFormatted,
        RoutingNumber,
        AccountNumberA,
        AccountNumberB,
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
        $"Contractor ITIN {ItinFormatted}. Amount 12.50 on 2024-11-25 to ACME Corp.";

    /// <summary>The values that must never appear anywhere after masking.</summary>
    public static IEnumerable<string> LeakTargets() => AllRaw.Concat(AllRawDigits.Where(d => d.Length >= 8));
}
