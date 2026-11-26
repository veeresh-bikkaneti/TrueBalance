using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects US Employer Identification Numbers (NN-NNNNNNN) and IRS-issued
/// ITINs (9NN-NN-NNNN with the 4th and 5th digits in the IRS ranges
/// 50-65, 70-88, 90-92, 94-99). Formatted forms and bare ITIN-shaped runs
/// are detected; a bare nine-digit run that only fits an EIN is ambiguous
/// with routing and account numbers and is left alone (documented gap).
/// </summary>
public sealed partial class EinItinDetector : IPiiDetector
{
    [GeneratedRegex(@"(?<![\d-])\d{2}-\d{7}(?![\d-])")]
    private static partial Regex FormattedEin();

    [GeneratedRegex(@"(?<![\d-])9\d{2}[- ]\d{2}[- ]\d{4}(?![\d-])")]
    private static partial Regex FormattedItin();

    [GeneratedRegex(@"(?<![\d-])9\d{8}(?![\d-])")]
    private static partial Regex BareItinShaped();

    public PiiType Type => PiiType.EinItin;

    public IEnumerable<PiiMatch> Find(string text)
    {
        foreach (Match match in FormattedEin().Matches(text))
        {
            yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
        }

        foreach (Match match in FormattedItin().Matches(text))
        {
            if (HasItinMiddleDigits(PiiText.OnlyDigits(match.Value)))
            {
                yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
            }
        }

        foreach (Match match in BareItinShaped().Matches(text))
        {
            if (HasItinMiddleDigits(PiiText.OnlyDigits(match.Value)))
            {
                yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
            }
        }
    }

    private static bool HasItinMiddleDigits(string digits)
    {
        var middle = int.Parse(digits.Substring(3, 2));
        return middle is (>= 50 and <= 65)
            or (>= 70 and <= 88)
            or (>= 90 and <= 92)
            or (>= 94 and <= 99);
    }
}
