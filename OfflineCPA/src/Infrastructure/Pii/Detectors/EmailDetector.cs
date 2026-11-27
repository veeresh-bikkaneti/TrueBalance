using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects email addresses: a local part, "@", and a domain with a dot. It is
/// a shape check, not an address validator. Local pattern only.
/// </summary>
public sealed partial class EmailDetector : IPiiDetector
{
    // The lookbehind starts a match only at the head of a word, which also
    // keeps a long run with no "@" from being rescanned at every position.
    [GeneratedRegex(@"(?<![\p{L}\p{N}._%+'\-])[\p{L}\p{N}._%+'\-]+@(?:[\p{L}\p{N}\-]+\.)+\p{L}{2,}")]
    private static partial Regex Address();

    public PiiType Type => PiiType.Email;

    public IEnumerable<PiiMatch> Find(string text)
    {
        foreach (Match match in Address().Matches(text))
        {
            yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
        }
    }
}
