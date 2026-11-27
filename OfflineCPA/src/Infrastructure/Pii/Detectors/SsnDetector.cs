using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects US Social Security Numbers, formatted (NNN-NN-NNNN) or as a
/// bare nine-digit run. Invalid ranges are skipped: area 000 and 666 are
/// never assigned, and 9xx belongs to the IRS-issued ITIN/ATIN range, so
/// those are left to <see cref="EinItinDetector"/>. No network, no lookup.
/// </summary>
public sealed partial class SsnDetector : IPiiDetector
{
    // Separator forms only: the same one of - space . _ / between the groups.
    [GeneratedRegex(@"(?<![0-9-])[0-9]{3}([- ._/])[0-9]{2}[0-9]{4}(?![0-9-])")]
    private static partial Regex FormattedSsn();

    [GeneratedRegex(@"(?<![0-9-])[0-9]{9}(?![0-9-])")]
    private static partial Regex BareNineDigits();

    public PiiType Type => PiiType.Ssn;

    public IEnumerable<PiiMatch> Find(string text)
    {
        foreach (Match match in FormattedSsn().Matches(text))
        {
            if (IsValidSsn(PiiText.OnlyDigits(match.Value)))
            {
                yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
            }
        }

        foreach (Match match in BareNineDigits().Matches(text))
        {
            if (IsValidSsn(PiiText.OnlyDigits(match.Value)))
            {
                yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
            }
        }
    }

    private static bool IsValidSsn(string digits)
    {
        var area = int.Parse(digits[..3]);
        var group = int.Parse(digits.Substring(3, 2));
        var serial = int.Parse(digits.Substring(5, 4));

        return area is not (000 or 666)
            && area is not (>= 900 and <= 999)
            && group != 0
            && serial != 0;
    }
}
