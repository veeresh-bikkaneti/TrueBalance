using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects US-style street addresses: a house number, one to three name
/// words, then a street suffix ("742 Example Street"). It is a shape check,
/// so it can also catch a non-address that looks the same; the policy masks
/// it either way. Local pattern only.
/// </summary>
public sealed partial class StreetAddressDetector : IPiiDetector
{
    [GeneratedRegex(@"\b[0-9]{1,6}\s+(?:[A-Za-z][A-Za-z.]*\s+){1,3}(?:Street|St|Avenue|Ave|Road|Rd|Boulevard|Blvd|Lane|Ln|Drive|Dr|Way|Court|Ct)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StreetLine();

    public PiiType Type => PiiType.StreetAddress;

    public IEnumerable<PiiMatch> Find(string text)
    {
        foreach (Match match in StreetLine().Matches(text))
        {
            yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
        }
    }
}
