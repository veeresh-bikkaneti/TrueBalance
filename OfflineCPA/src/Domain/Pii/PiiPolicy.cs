using System.Security.Cryptography;
using System.Text;

namespace Domain.Pii;

/// <summary>
/// The masking policy from ADR 0010: before statement text is stored,
/// logged, or echoed in an error, every detected value is replaced by a
/// non-destructive token holding the last four digits and a keyed
/// fingerprint, so matching still works and the full value never persists.
/// The fingerprint prefix embedded in the token is enough for row-to-row
/// matching; <see cref="MaskedValue.Fingerprint"/> carries the full digest.
/// </summary>
public sealed class PiiPolicy
{
    private const int FingerprintTokenLength = 16;

    // Lower priority number wins an overlapping span. Routing numbers have
    // a check digit, so they outrank a plain nine-digit read; bank account
    // patterns are the most greedy, so they go last.
    private static readonly IReadOnlyDictionary<PiiType, int> Priority = new Dictionary<PiiType, int>
    {
        [PiiType.AbaRoutingNumber] = 0,
        [PiiType.Ssn] = 1,
        [PiiType.EinItin] = 2,
        [PiiType.BankAccountNumber] = 3,
    };

    private static readonly IReadOnlyDictionary<PiiType, string> TypeToken = new Dictionary<PiiType, string>
    {
        [PiiType.Ssn] = "SSN",
        [PiiType.EinItin] = "EIN",
        [PiiType.AbaRoutingNumber] = "ABA",
        [PiiType.BankAccountNumber] = "ACCT",
    };

    private const string UnknownTypeToken = "PII";

    private readonly byte[] _key;
    private readonly IReadOnlyList<IPiiDetector> _detectors;

    public PiiPolicy(byte[] key, IReadOnlyList<IPiiDetector> detectors)
    {
        _key = (byte[])key.Clone();
        _detectors = detectors;
    }

    public PiiMaskingResult Mask(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Type priority first, then position: a check-digit routing number or
        // a formatted SSN that starts later must still outrank a greedy
        // account run that swallows it. Within one type, the earliest match
        // claims its span.
        var candidates = _detectors
            .SelectMany((detector, index) => detector.Find(text).Select(match => (DetectorIndex: index, Match: match)))
            .OrderBy(c => Priority.GetValueOrDefault(c.Match.Type, int.MaxValue))
            .ThenBy(c => c.Match.Start)
            .ThenByDescending(c => c.Match.Length)
            .ThenBy(c => c.DetectorIndex)
            .ToList();

        // First match claims its span; overlapping later matches are dropped.
        // Claiming runs by priority, so rebuild the text in text order.
        var claimed = new List<PiiMatch>();
        var spans = new List<(int Start, int End)>();
        foreach (var candidate in candidates)
        {
            var end = candidate.Match.Start + candidate.Match.Length;
            if (spans.Any(s => candidate.Match.Start < s.End && end > s.Start))
            {
                continue;
            }

            claimed.Add(candidate.Match);
            spans.Add((candidate.Match.Start, end));
        }

        claimed.Sort((a, b) => a.Start.CompareTo(b.Start));

        var masked = new StringBuilder(text.Length);
        var matches = new List<MaskedValue>(claimed.Count);
        var cursor = 0;
        foreach (var match in claimed)
        {
            masked.Append(text[cursor..match.Start]);
            masked.Append(TokenFor(match));
            cursor = match.Start + match.Length;

            matches.Add(new MaskedValue(
                match.Type,
                DigitsOf(match.RawValue)[^4..],
                Fingerprint(match.RawValue),
                TokenFor(match)));
        }

        masked.Append(text[cursor..]);
        return new PiiMaskingResult(masked.ToString(), matches);
    }

    /// <summary>HMAC-SHA256 over the digits only, so "123-45-6789" and
    /// "123456789" produce the same fingerprint and still match.</summary>
    public string Fingerprint(string rawValue)
    {
        using var hmac = new HMACSHA256(_key);
        var digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(DigitsOf(rawValue)));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private string TokenFor(PiiMatch match)
    {
        var digits = DigitsOf(match.RawValue);
        var last4 = digits[^4..];
        var fingerprint = Fingerprint(match.RawValue)[..FingerprintTokenLength];
        return $"[{TypeToken.GetValueOrDefault(match.Type, UnknownTypeToken)} ****{last4} fp:{fingerprint}]";
    }

    private static string DigitsOf(string value)
    {
        var digits = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsDigit(c))
            {
                digits.Append(c);
            }
        }

        return digits.ToString();
    }
}
