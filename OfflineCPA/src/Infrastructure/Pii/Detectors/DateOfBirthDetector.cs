using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects a date of birth only when a label says so ("DOB", "D.O.B.", "date
/// of birth", "born on"), so ordinary dates in statements stay readable. The
/// match is the date after the label, not the label. Local pattern only.
/// </summary>
public sealed partial class DateOfBirthDetector : IPiiDetector
{
    [GeneratedRegex(@"(?<=\b(?:DOB|D\.O\.B\.?|date of birth|born on)\s*:?\s*)(?:[0-9]{1,2}[/.-][0-9]{1,2}[/.-][0-9]{4}|[0-9]{4}-[0-9]{2}-[0-9]{2})", RegexOptions.IgnoreCase)]
    private static partial Regex LabeledDate();

    public PiiType Type => PiiType.DateOfBirth;

    public IEnumerable<PiiMatch> Find(string text)
    {
        foreach (Match match in LabeledDate().Matches(text))
        {
            yield return new PiiMatch(Type, match.Value, match.Index, match.Length);
        }
    }
}
