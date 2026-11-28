using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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
    private const int LongestSingleValue = 17;

    // Lower priority number wins an overlapping span. Routing numbers have
    // a check digit, so they outrank a plain nine-digit read; cards pass a
    // Luhn check, so they come next. Bank account patterns are the most
    // greedy, so they go last. An email goes first, because digits inside it
    // would otherwise be claimed alone and leave the rest readable. Phone
    // shapes rank just above account numbers.
    private static readonly IReadOnlyDictionary<PiiType, int> Priority = new Dictionary<PiiType, int>
    {
        [PiiType.Email] = 0,
        [PiiType.AbaRoutingNumber] = 1,
        [PiiType.Card] = 2,
        [PiiType.Ssn] = 3,
        [PiiType.EinItin] = 4,
        [PiiType.DateOfBirth] = 5,
        [PiiType.StreetAddress] = 5,
        [PiiType.Phone] = 6,
        [PiiType.BankAccountNumber] = 7,
    };

    private static readonly IReadOnlyDictionary<PiiType, string> TypeToken = new Dictionary<PiiType, string>
    {
        [PiiType.Ssn] = "SSN",
        [PiiType.EinItin] = "EIN",
        [PiiType.AbaRoutingNumber] = "ABA",
        [PiiType.BankAccountNumber] = "ACCT",
        [PiiType.Card] = "CARD",
        [PiiType.DateOfBirth] = "DOB",
        [PiiType.StreetAddress] = "ADDR",
        [PiiType.Phone] = "PHONE",
        [PiiType.Email] = "EMAIL",
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
        // Detectors read an ASCII-digit copy of the same length, so their
        // spans index straight into the original text.
        var ascii = ToAsciiDigits(text);
        var candidates = _detectors
            .SelectMany((detector, index) => detector.Find(ascii).Select(match => (DetectorIndex: index, Match: match)))
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

        CoverLongRuns(text, claimed, candidates.Select(c => c.Match));
        CoverLeadingDigits(text, claimed, candidates.Select(c => c.Match));
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
                Last4Of(match),
                FingerprintOf(match),
                TokenFor(match)));
        }

        masked.Append(text[cursor..]);
        return new PiiMaskingResult(masked.ToString(), matches);
    }

    // A separated run too long to be one value may overlap shorter claims.
    // If those leave any of its digits readable, one token replaces them all,
    // widened to cover any claim that sticks out past the run.
    private static void CoverLongRuns(string text, List<PiiMatch> claimed, IEnumerable<PiiMatch> candidates)
    {
        var longRuns = candidates.Where(m =>
            m.Type == PiiType.BankAccountNumber && DigitsOf(m.RawValue).Length > LongestSingleValue);

        foreach (var run in longRuns)
        {
            var start = run.Start;
            var end = start + run.Length;
            var exposed = Enumerable.Range(start, run.Length).Any(i =>
                char.IsDigit(text[i]) && !claimed.Any(c => i >= c.Start && i < c.Start + c.Length));
            if (!exposed)
            {
                continue;
            }

            var overlapping = claimed.Where(c => c.Start < end && c.Start + c.Length > run.Start).ToList();
            foreach (var match in overlapping)
            {
                start = Math.Min(start, match.Start);
                end = Math.Max(end, match.Start + match.Length);
                claimed.Remove(match);
            }

            claimed.Add(new PiiMatch(run.Type, text[start..end], start, end - start));
        }
    }

    // A phone or address that starts inside a grouped number takes its last
    // group, and the account claim for the whole run is dropped as an overlap.
    // The phone or address claim is then widened back over the earlier groups.
    // It keeps its type, so the token still shows no digits.
    private static void CoverLeadingDigits(string text, List<PiiMatch> claimed, IEnumerable<PiiMatch> candidates)
    {
        foreach (var run in candidates.Where(m => m.Type == PiiType.BankAccountNumber))
        {
            var head = claimed.Find(c => c.Type is PiiType.Phone or PiiType.StreetAddress
                && c.Start > run.Start && c.Start < run.Start + run.Length);
            if (head is null)
            {
                continue;
            }

            var exposed = Enumerable.Range(run.Start, head.Start - run.Start).Any(i =>
                char.IsDigit(text[i]) && !claimed.Any(c => i >= c.Start && i < c.Start + c.Length));
            if (!exposed)
            {
                continue;
            }

            var end = head.Start + head.Length;
            claimed.RemoveAll(c => c.Start < end && c.Start + c.Length > run.Start);
            claimed.Add(new PiiMatch(head.Type, text[run.Start..end], run.Start, end - run.Start));
        }
    }

    /// <summary>HMAC-SHA256 over the digits only, so "123-45-6789" and
    /// "123456789" produce the same fingerprint and still match.</summary>
    public string Fingerprint(string rawValue) => Hmac(DigitsOf(ToAsciiDigits(rawValue)));

    // Emails and street addresses are matched on their lowercased text: digits
    // alone would give unrelated values the same fingerprint.
    private string FingerprintOf(PiiMatch match) => match.Type switch
    {
        PiiType.Email => Hmac(match.RawValue.ToLowerInvariant()),
        PiiType.StreetAddress => Hmac(NormalizeAddress(match.RawValue)),
        _ => Fingerprint(match.RawValue),
    };

    // Case, spacing and punctuation vary between statements for one address.
    private static string NormalizeAddress(string value) =>
        Regex.Replace(ToAsciiDigits(value).ToLowerInvariant(), @"[\s\p{P}]+", " ").Trim();

    private string Hmac(string value)
    {
        using var hmac = new HMACSHA256(_key);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private string TokenFor(PiiMatch match)
    {
        var fingerprint = FingerprintOf(match)[..FingerprintTokenLength];
        var label = TypeToken.GetValueOrDefault(match.Type, UnknownTypeToken);
        var visible = KeepsLast4(match.Type) ? $"****{Last4Of(match)} " : string.Empty;
        return $"[{label} {visible}fp:{fingerprint}]";
    }

    // Numbers keep their last four digits for display and matching. A date of
    // birth, street address, phone number or email keeps none: any readable
    // part would narrow the value down, so only the fingerprint stays.
    private static bool KeepsLast4(PiiType type) =>
        type is not (PiiType.DateOfBirth or PiiType.StreetAddress or PiiType.Phone or PiiType.Email);

    private static string Last4Of(PiiMatch match) =>
        KeepsLast4(match.Type) ? DigitsOf(match.RawValue)[^4..] : string.Empty;

    private static string ToAsciiDigits(string text) =>
        string.Create(text.Length, text, static (copy, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                copy[i] = char.IsDigit(c) ? (char)('0' + (int)char.GetNumericValue(c)) : c;
            }
        });

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
