using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects bank account numbers: a run of 8 to 17 digits, optionally split
/// into groups by single hyphens or spaces. Because there is no check digit,
/// this is the least certain detector: it excludes phone-shaped numbers,
/// YYYYMMDD-style dates, currency amounts, tax-id shapes, and longer runs
/// that belong to other detectors. A separated run longer than 17 digits is
/// offered whole, and <see cref="PiiPolicy"/> masks all of it if any digit
/// would otherwise stay readable. Known false
/// positives (order or invoice numbers with eight or more digits) are
/// documented in docs/privacy/masking-coverage.md.
/// </summary>
public sealed partial class BankAccountNumberDetector : IPiiDetector
{
    private const int MinDigits = 8;
    private const int MaxDigits = 17;

    [GeneratedRegex(@"[0-9][0-9 \-]*[0-9]")]
    private static partial Regex DigitRun();

    // Dot, underscore or slash grouping: one separator kind, at least two of
    // them, so decimal amounts ("12345678.90") never qualify.
    [GeneratedRegex(@"[0-9]+(?:([._/])[0-9]+)(?:\1[0-9]+)+")]
    private static partial Regex PunctuatedRun();

    // US phone shape: 555-123-4567 or 555 123 4567 (3-3-4 groups).
    [GeneratedRegex(@"^[0-9]{3}[- ._/][0-9]{3}[- ._/][0-9]{4}$")]
    private static partial Regex PhoneShape();

    // Typed tax-id shapes that failed their own validation: NN-NNNNNNN or
    // NNN-NN-NNNN. Relabeling those as account numbers would be a wrong
    // type, so they stay untouched instead.
    [GeneratedRegex(@"^[0-9]{2}[-._/][0-9]{7}$|^[0-9]{3}[- ._/][0-9]{2}[- ._/][0-9]{4}$")]
    private static partial Regex TaxIdShape();

    public PiiType Type => PiiType.BankAccountNumber;

    public IEnumerable<PiiMatch> Find(string text) => FindSpaced(text).Concat(FindPunctuated(text));

    private IEnumerable<PiiMatch> FindSpaced(string text)
    {
        foreach (Match match in DigitRun().Matches(text))
        {
            foreach (var (candidate, offset) in CandidatesFor(match.Value))
            {
                var start = match.Index + offset;
                if (IsPlausibleAccountNumber(text, candidate, start))
                {
                    yield return new PiiMatch(Type, candidate, start, candidate.Length);
                }
            }
        }
    }

    private static IEnumerable<PiiMatch> FindPunctuated(string text)
    {
        foreach (Match match in PunctuatedRun().Matches(text))
        {
            var end = match.Index + match.Length;

            // "12.345.678,90" is a European-style amount.
            var isAmount = end + 1 < text.Length && text[end] == ',' && char.IsAsciiDigit(text[end + 1]);
            if (isAmount)
            {
                continue;
            }

            // The whole run, then the run after its first separator. An account
            // that follows an SSN ("123-45-6789_1234_5678_9012") starts inside
            // the SSN's claim, so only this tail can survive the overlap check.
            var firstBreak = match.Value.IndexOfAny(['.', '_', '/']) + 1;
            var candidates = new[]
            {
                (Value: match.Value, Start: match.Index),
                (Value: match.Value[firstBreak..], Start: match.Index + firstBreak),
            };

            foreach (var (value, start) in candidates)
            {
                if (PiiText.OnlyDigits(value).Length >= MinDigits
                    && IsPlausibleAccountNumber(text, value, start))
                {
                    yield return new PiiMatch(PiiType.BankAccountNumber, value, start, value.Length);
                }
            }
        }
    }

    // Space-only groups, five or more: a list of numbers ("Invoices 1001 1002
    // 1003 1004 1005"), not one account, so the whole run is not offered.
    private static bool IsSpaceList(string run) =>
        run.Contains(' ') && !run.Contains("  ") && !run.Contains('-') && run.Split(' ').Length > 4;

    // Double spaces, "--" or " - " break a run apart.
    private static bool IsBrokenRun(string candidate) =>
        candidate.Contains("  ") || candidate.Contains("--") || candidate.Contains(" - ");

    // Any date-shaped group inside a run: 2024-11-25 or 25-11-2024 style.
    [GeneratedRegex(@"[0-9]{4}[-._/ ][0-9]{2}[-._/ ][0-9]{2}|[0-9]{2}[-._/ ][0-9]{2}[-._/ ][0-9]{4}")]
    private static partial Regex DateFragment();

    private static IEnumerable<(string Candidate, int Offset)> CandidatesFor(string run)
    {
        var digits = PiiText.OnlyDigits(run);
        var hasSeparators = run.Any(c => c is ' ' or '-');

        // The whole run is the candidate when it fits, or when it is a
        // separated run too long to be one value: grouped
        // accounts ("1234 5678 9012") rely on this candidate. Chunks are
        // also offered, so values pushed together ("12345678 123-45-6789")
        // are still masked per value when a higher-priority detector claims
        // its span and the policy drops the whole-run candidate. Overlap
        // resolution keeps at most one of them.
        if (digits.Length >= MinDigits && (digits.Length <= MaxDigits || !IsSpaceList(run)))
        {
            yield return (run, 0);
        }

        if (!hasSeparators)
        {
            yield break;
        }

        var start = 0;
        for (var i = 0; i <= run.Length; i++)
        {
            if (i < run.Length && run[i] is not (' ' or '-'))
            {
                continue;
            }

            if (i > start)
            {
                var chunk = run[start..i];
                if (PiiText.OnlyDigits(chunk).Length is >= MinDigits and <= MaxDigits)
                {
                    yield return (chunk, start);
                }
            }

            start = i + 1;
        }
    }

    private static bool IsPlausibleAccountNumber(string text, string candidate, int start)
    {
        var digits = PiiText.OnlyDigits(candidate);

        // Two dates joined by a range dash ("2024-11-25 - 2024-12-01") are prose.
        if (IsBrokenRun(candidate) && DateFragment().IsMatch(candidate))
        {
            return false;
        }

        var before = start > 0 ? text[start - 1] : '\0';
        var afterIndex = start + candidate.Length;
        var after = afterIndex < text.Length ? text[afterIndex] : '\0';
        if (char.IsDigit(before) || char.IsDigit(after))
        {
            return false;
        }

        // Decimal neighbors mean an amount ("12345678.90"), but a file
        // extension ("12345678.pdf") is not one.
        if (before == '.' && start >= 2 && char.IsDigit(text[start - 2]))
        {
            return false;
        }

        if (after == '.' && afterIndex + 1 < text.Length && char.IsDigit(text[afterIndex + 1]))
        {
            return false;
        }

        // Currency amounts: $1,234.56 style neighbors, $12345678.90.
        var preceding = PrecedingNonWhitespace(text, start);
        if (preceding is '$' or '£' or '€')
        {
            return false;
        }

        if (digits.Length == 10 && PhoneShape().IsMatch(candidate))
        {
            return false;
        }

        if (digits.Length == 9 && TaxIdShape().IsMatch(candidate))
        {
            return false;
        }

        if (DotGroupedAmount().IsMatch(candidate))
        {
            return false;
        }

        if (DateShape().IsMatch(candidate) || (digits.Length == 8 && IsBareDate(digits)))
        {
            return false;
        }

        return true;
    }

    // 12.345.678: dot-grouped thousands.
    [GeneratedRegex(@"^[0-9]{1,3}(?:\.[0-9]{3}){2,}$")]
    private static partial Regex DotGroupedAmount();

    // ISO, US or day-first date shape: 2024-11-25, 11.25.2024, 25/11/2024.
    [GeneratedRegex(@"^(?:(18|19|20|21)[0-9]{2}(?<a>[- ._/])(0[1-9]|1[0-2])\k<a>(0[1-9]|[12][0-9]|3[01])|(0[1-9]|1[0-2])(?<b>[- ._/])(0[1-9]|[12][0-9]|3[01])\k<b>(18|19|20|21)[0-9]{2}|(0[1-9]|[12][0-9]|3[01])(?<c>[./])(0[1-9]|1[0-2])\k<c>(18|19|20|21)[0-9]{2})$")]
    private static partial Regex DateShape();

    private static char PrecedingNonWhitespace(string text, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return text[i];
            }
        }

        return '\0';
    }

    private static bool IsBareDate(string digits)
    {
        var year = int.Parse(digits[..4]);
        var month = int.Parse(digits.Substring(4, 2));
        var day = int.Parse(digits.Substring(6, 2));
        return year is >= 1800 and <= 2199
            && month is >= 1 and <= 12
            && day is >= 1 and <= 31;
    }
}
