using System.Net;
using System.Text.RegularExpressions;
using Application.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Tolerant tag scan of STMTTRN blocks, so SGML (unclosed tags) and XML files
/// both work. Only date, amount, name and memo are read; account ids are never touched.
/// </summary>
public sealed class OfxStatementImporter : IStatementImporter
{
    public bool SupportsExtension(string extension)
        => extension.Equals(".ofx", StringComparison.OrdinalIgnoreCase)
           || extension.Equals(".qfx", StringComparison.OrdinalIgnoreCase);

    public async Task<ParsedStatement> ParseAsync(string filePath, CsvColumnMap? map, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        var blocks = Regex.Split(text, "<STMTTRN>", RegexOptions.IgnoreCase);
        var rows = new List<ParsedRow>();
        for (var i = 1; i < blocks.Length; i++)
        {
            var block = Regex.Split(blocks[i], "</STMTTRN>|</BANKTRANLIST>", RegexOptions.IgnoreCase)[0];
            string Tag(string name)
                => WebUtility.HtmlDecode(
                    Regex.Match(block, $@"<{name}>([^<\r\n]*)", RegexOptions.IgnoreCase).Groups[1].Value).Trim();

            var posted = Tag("DTPOSTED");
            var date = StatementValues.Date(posted.Length >= 8 ? posted[..8] : posted, "yyyyMMdd", i, "date");
            var description = string.Join(' ', new[] { Tag("NAME"), Tag("MEMO") }.Where(s => s.Length > 0));
            rows.Add(new ParsedRow(i, date, description, StatementValues.Amount(Tag("TRNAMT"), i, "amount"), null));
        }
        return new ParsedStatement(rows, false);
    }
}
