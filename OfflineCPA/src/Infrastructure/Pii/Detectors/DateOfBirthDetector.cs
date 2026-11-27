using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects a date of birth only when a label says so ("DOB", "D.O.B.", "date
/// of birth", "birth date", "birthdate", "born", "born on", with or without a
/// colon or dash), so ordinary dates in statements stay readable. Years may
/// have two or four digits, and months may be English names or their
/// three-letter forms. The match is the date after the label, not the label.
/// Local pattern only.
/// </summary>
public sealed partial class DateOfBirthDetector : IPiiDetector
{
    private const string Label = @"(?<=\b(?:DOB|D\.O\.B\.?|date of birth|birth ?date|born(?: on)?)\s*(?:[:\-\u2013\u2014]\s*)?)";

    private const string Month =
        "(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|June?|July?|Aug(?:ust)?|" +
        "Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)";

    private const string Day = "[0-9]{1,2}(?:st|nd|rd|th)?";
    private const string Year = "(?:[0-9]{4}|[0-9]{2})(?![0-9])";

    [GeneratedRegex(
        Label + "(?:[0-9]{4}-[0-9]{2}-[0-9]{2}"
        + "|[0-9]{1,2}[/.-][0-9]{1,2}[/.-]" + Year
        + "|" + Month + @"\b\.?[\s-]*" + Day + @"(?:\s*,\s*|[\s-]+)" + Year
        + "|" + Day + @"[\s-]*" + Month + @"\b\.?,?[\s-]*" + Year + ")",
        RegexOptions.IgnoreCase)]
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
