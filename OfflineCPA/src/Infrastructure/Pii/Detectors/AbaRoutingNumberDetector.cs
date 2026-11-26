using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects ABA routing transit numbers: a bare nine-digit run whose check
/// digit validates (weights 3,7,1,3,7,1,3,7,1; the weighted sum must be a
/// multiple of 10). Checksum-valid runs outrank SSN reads, which is why the
/// policy gives this detector first claim on overlapping spans.
/// </summary>
public sealed partial class AbaRoutingNumberDetector : IPiiDetector
{
    private static readonly int[] Weights = [3, 7, 1, 3, 7, 1, 3, 7, 1];

    [GeneratedRegex(@"(?<![\d-])\d{9}(?![\d-])")]
    private static partial Regex BareNineDigits();

    public PiiType Type => PiiType.AbaRoutingNumber;

    public IEnumerable<PiiMatch> Find(string text)
    {
        foreach (Match match in BareNineDigits().Matches(text))
        {
            if (PassesChecksum(PiiText.OnlyDigits(match.Value)))
            {
                yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
            }
        }
    }

    private static bool PassesChecksum(string digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += (digits[i] - '0') * Weights[i];
        }

        return sum % 10 == 0;
    }
}
