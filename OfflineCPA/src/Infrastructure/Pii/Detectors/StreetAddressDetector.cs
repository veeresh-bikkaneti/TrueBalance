using System.Text.RegularExpressions;
using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// Detects US-style street addresses: a house number, one to five name words
/// (ordinals such as "5th" included, common English function words such as
/// "the", "for" or "via" excluded), then a street suffix, rural and highway
/// contract routes with a box number, and PO boxes. A unit after a street or
/// PO box ("Apt 4") is part of the match. It is a shape check, so it can also
/// catch a non-address that looks the same; masking too much is the safe
/// direction. Local pattern only.
/// The suffix list holds the names and abbreviations in USPS Publication 28
/// Appendix C1 (https://pe.usps.com/text/pub28/28apc_002.htm) [Moderate 1/1],
/// except "C", "Un", "Is", "Iss" and "Via", which read as ordinary memo text:
/// addresses on an island or viaduct written only with those short forms are
/// no longer caught.
/// Route shapes follow sections 241 and 251, "RR ## BOX ##" and
/// "HC ## BOX ##" (https://pe.usps.com/text/pub28/28c2_021.htm,
/// https://pe.usps.com/text/pub28/28c2_027.htm) [Moderate 1/1].
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
        "Village|Vlg|Vista|Vis|Walk|Way|" +
        "Allee|Ally|Anex|Annex|Annx|Anx|Arc|Arcade|Av|Aven|Avenu|Avn|Avnue|Bayoo|Bayou|Bch|Beach|Bg|" +
        "Bgs|Blf|Blfs|Bluf|Bluff|Bluffs|Bot|Bottm|Bottom|Boul|Boulv|Br|Branch|Brdge|Brg|Bridge|Brk|" +
        "Brks|Brnch|Brook|Brooks|Btm|Burg|Burgs|Bypa|Bypas|Byps|Byu|Camp|Canyn|Canyon|Cape|Causwa|" +
        "Cen|Cent|Centers|Centr|Centre|Circ|Circl|Circles|Cirs|Clb|Clf|Clfs|Cliff|Cliffs|Club|Cmn|" +
        "Cmp|Cnter|Cntr|Cnyn|Common|Cor|Corner|Corners|Cors|Course|Courts|Coves|Cp|Cpe|Crcl|Crcle|" +
        "Crest|Crossroad|Crossroads|Crse|Crsent|Crsnt|Crssng|Crst|Ctrs|Cts|Curv|Curve|Cvs|Cyn|Dale|" +
        "Dam|Div|Divide|Dl|Dm|Driv|Drives|Drs|Drv|Dv|Dvd|Est|Estate|Exp|Expr|Express|Expw|Ext|" +
        "Extension|Extensions|Extn|Extnsn|Exts|Fall|Falls|Ferry|Field|Fields|Flat|Flats|Fld|Flds|" +
        "Fls|Flt|Flts|Ford|Fords|Forest|Forests|Forg|Forge|Forges|Fork|Forks|Fort|Frd|Frds|Freewy|" +
        "Frg|Frgs|Frk|Frks|Frry|Frst|Frt|Frway|Frwy|Fry|Ft|Garden|Gardn|Gateway|Gatewy|Gatway|Gdn|" +
        "Glens|Glns|Grden|Grdn|Grdns|Greens|Grns|Grov|Groves|Grvs|Gtway|Gtwy|Harb|Harbor|Harbors|" +
        "Harbr|Haven|Hbr|Hbrs|Highwy|Hiway|Hiwy|Hllw|Hollows|Holws|Hrbor|Ht|Hvn|Hway|Inlet|Inlt|" +
        "Island|Islands|Isle|Isles|Islnd|Islnds|Jct|Jction|Jctn|Jctns|Jcts|Junction|Junctions|" +
        "Junctn|Juncton|Key|Keys|Knl|Knls|Knol|Knoll|Knolls|Ky|Kys|Lake|Lakes|Land|Lck|Lcks|Ldg|" +
        "Ldge|Lf|Lgt|Lgts|Light|Lights|Lk|Lks|Lndng|Loaf|Lock|Locks|Lodg|Lodge|Loops|Mall|Manors|" +
        "Mdw|Meadow|Medows|Mill|Mills|Mission|Missn|Ml|Mls|Mnrs|Mnt|Mntain|Mntn|Mntns|Motorway|" +
        "Mount|Mountain|Mountains|Mountin|Msn|Mssn|Mt|Mtin|Mtn|Mtns|Mtwy|Nck|Neck|Opas|Orch|Orchard|" +
        "Orchrd|Oval|Overpass|Ovl|Parks|Parkways|Parkwy|Passage|Paths|Pikes|Pine|Pines|Pkway|Pkwys|" +
        "Pky|Plain|Plains|Pln|Plns|Plza|Pne|Pnes|Points|Port|Ports|Pr|Prairie|Prk|Prr|Prt|Prts|Psge|" +
        "Pts|Rad|Radial|Radiel|Radl|Ramp|Ranch|Ranches|Rapid|Rapids|Rdge|Rdgs|Rds|Rest|Ridges|Riv|" +
        "River|Rivr|Rnch|Rnchs|Roads|Rpd|Rpds|Rst|Rue|Rvr|Shl|Shls|Shoal|Shoals|Shoar|Shoars|Shore|" +
        "Shores|Shr|Shrs|Skwy|Skyway|Smt|Spg|Spgs|Spng|Spngs|Spring|Springs|Sprng|Sprngs|Spur|Spurs|" +
        "Sqr|Sqre|Sqrs|Sqs|Squ|Squares|Statn|Stn|Str|Stra|Strav|Straven|Stravenue|Stravn|Stream|" +
        "Streets|Streme|Strm|Strt|Strvn|Strvnue|Sts|Sumit|Sumitt|Summit|Terr|Throughway|Traces|" +
        "Track|Tracks|Trafficway|Trailer|Trails|Trak|Trfy|Trk|Trks|Trlr|Trlrs|Trls|Trnpk|Trwy|Tunel|" +
        "Tunl|Tunls|Tunnel|Tunnels|Tunnl|Turnpk|Underpass|Union|Unions|Uns|Upas|Valley|Valleys|" +
        "Vally|Vdct|Viadct|Viaduct|Views|Vill|Villag|Villages|Ville|Villg|Villiage|Vist|Vl|Vlgs|" +
        "Vlly|Vly|Vlys|Vst|Vsta|Vws|Walks|Wall|Ways|Well|Wells|Wl|Wls|Wy|Xrd|Xrds";

    private const string Unit = @"(?:\.?\s*,?\s*(?:(?:Apt|Suite|Ste|Unit)\b\.?\s*#?|#)\s*[A-Za-z0-9][A-Za-z0-9-]*)?";

    // A memo such as "12 invoices this is late" is held together by these words;
    // a street name rarely is. The word must end at a space, so "A." still counts
    // as an initial.
    private const string FunctionWord = @"(?:a|an|the|to|for|of|at|from|via|in|on|by|with|and|or|this|that|is|was|per)\s";

    private const string NameWord = @"(?!" + FunctionWord + @")(?:[A-Za-z][A-Za-z.'\u2019\-]*|[0-9]{1,3}(?:st|nd|rd|th))";

    private const string Route = @"(?:Rural\s+Route|R\.?R\.?|Highway\s+Contract(?:\s+Route)?|H\.?C\.?)";

    [GeneratedRegex(
        @"\b[0-9]{1,6}\s+(?:" + NameWord + @"\s+){1,5}(?:" + Suffix + @")\b" + Unit
        + @"|\b" + Route + @"\s*#?\s*[0-9]+\s*,?\s*Box\s*#?\s*[0-9][A-Za-z0-9-]*"
        + @"|\bP\.?\s*O\.?\s*Box\s*#?\s*[0-9]+" + Unit,
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
