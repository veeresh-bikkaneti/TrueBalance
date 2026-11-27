using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects US-style street addresses: a house number, one to three name
/// words, then a street suffix ("742 Example Street"), plus any unit after it
/// ("Apt 4"), and PO boxes. It is a shape check, so it can also catch a
/// non-address that looks the same; masking too much is the safe direction.
/// Local pattern only.
/// The suffixes are a common subset of the names and standard abbreviations in
/// USPS Publication 28 Appendix C1
/// (https://pe.usps.com/text/pub28/28apc_002.htm) [Moderate 1/1].
/// </summary>
public sealed partial class StreetAddressDetector : IPiiDetector
{
    private const string Suffix =
        "Alley|Aly|Avenue|Ave|Bend|Bnd|Boulevard|Blvd|Bypass|Byp|Causeway|Cswy|Center|Ctr|Circle|Cir|" +
        "Commons|Cmns|Court|Ct|Cove|Cv|Creek|Crk|Crescent|Cres|Crossing|Xing|Drive|Dr|Estates|Ests|" +
        "Expressway|Expy|Freeway|Fwy|Gardens|Gdns|Glen|Gln|Green|Grn|Grove|Grv|Heights|Hts|Highway|Hwy|" +
        "Hills|Hls|Hill|Hl|Hollow|Holw|Landing|Lndg|Lane|Ln|Loop|Manor|Mnr|Meadows|Mdws|Mews|Park|" +
        "Parkway|Pkwy|Pass|Path|Pike|Place|Pl|Plaza|Plz|Point|Pt|Ridge|Rdg|Road|Rd|Route|Rte|Row|Run|" +
        "Square|Sq|Station|Sta|Street|St|Terrace|Ter|Trace|Trce|Trail|Trl|Turnpike|Tpke|View|Vw|" +
        "Village|Vlg|Vista|Vis|Walk|Way";

    private const string Unit = @"(?:\.?\s*,?\s*(?:(?:Apt|Suite|Ste|Unit)\b\.?\s*#?|#)\s*[A-Za-z0-9][A-Za-z0-9-]*)?";

    [GeneratedRegex(
        @"\b[0-9]{1,6}\s+(?:[A-Za-z][A-Za-z.]*\s+){1,3}(?:" + Suffix + @")\b" + Unit
        + @"|\bP\.?\s*O\.?\s*Box\s*#?\s*[0-9]+",
        RegexOptions.IgnoreCase)]
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
