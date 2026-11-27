using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects payment card numbers: 13 to 19 digits, optionally grouped by single
/// spaces or hyphens, that pass the Luhn check digit. A card-shaped run that
/// fails Luhn is not claimed here, so the bank account detector still sees it.
/// Local arithmetic only: no card brand lookup, no network.
/// </summary>
public sealed partial class CardNumberDetector : IPiiDetector
{
    [GeneratedRegex(@"(?<![0-9])[0-9](?:[ -]?[0-9]){11,18}(?![0-9])")]
    private static partial Regex CardRun();

    public PiiType Type => PiiType.Card;

    public IEnumerable<PiiMatch> Find(string text)
    {
        foreach (Match match in CardRun().Matches(text))
        {
            var digits = PiiText.OnlyDigits(match.Value);
            if (digits.Length is >= 13 and <= 19 && PassesLuhn(digits))
            {
                yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
            }
        }
    }

    // Luhn: from the right, double every second digit and subtract 9 when the
    // doubled value passes 9; the total must end in zero.
    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var doubleThis = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            if (doubleThis)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleThis = !doubleThis;
        }

        return sum % 10 == 0;
    }
}
