using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects US phone numbers: 10 digits split by dashes, dots, spaces or area
/// code brackets, with an optional "+1" or "1" and an optional extension.
/// Ten bare digits stay with the account detector unless "+1" leads them.
/// A shape that touches more digits is left to the account detector, which
/// sees the whole run, so no neighboring group stays readable. Local pattern
/// only.
/// </summary>
public sealed partial class PhoneNumberDetector : IPiiDetector
{
    // Bracketed area code, or "+1": unlikely to be part of a longer number.
    private const string Marked =
        @"(?<![0-9])(?:(?:\+?1[-. ]?)?\([0-9]{3}\) ?[0-9]{3}[-. ]?[0-9]{4}"
        + @"|\+1[-. ]?[0-9]{3}[-. ]?[0-9]{3}[-. ]?[0-9]{4})(?![0-9])";

    // One repeated dash or dot: only the same glue can extend the number.
    private const string Glued =
        @"(?<![0-9][-.]?)(?:1[-. ])?[0-9]{3}(?<glue>[-.])[0-9]{3}\k<glue>[0-9]{4}(?![-.]?[0-9])";

    // Any space inside: a digit nearby may belong to the same grouped number.
    private const string Spaced =
        @"(?<![0-9][-. ]{0,3})(?:1[-. ])?[0-9]{3}[-. ][0-9]{3}[-. ][0-9]{4}(?![-. ]{0,3}[0-9])";

    private const string Extension = @"(?: ?(?:ext\.?|extension|x|#) ?[0-9]{1,6})?";

    [GeneratedRegex("(?:" + Marked + "|" + Glued + "|" + Spaced + ")" + Extension, RegexOptions.IgnoreCase)]
    private static partial Regex PhoneNumber();

    public PiiType Type => PiiType.Phone;

    public IEnumerable<PiiMatch> Find(string text)
    {
        foreach (Match match in PhoneNumber().Matches(text))
        {
            yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
        }
    }
}
