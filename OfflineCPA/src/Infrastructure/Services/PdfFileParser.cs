using System.Text.RegularExpressions;
using Application.Interfaces;
using UglyToad.PdfPig;

namespace Infrastructure.Services;

/// <summary>
/// Experimental PDF statements: PDF text has no reliable columns, so a row is a
/// guess ("date description amount" on one line) and a person must confirm it.
/// </summary>
public class PdfFileParser : IStatementImporter
{
    private static readonly Regex Line = new(
        @"^\s*(\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}/\d{4})\s+(.+?)\s+(\(?-?\$?[\d,]+\.\d{2}\)?)(?:\s+(\(?-?\$?[\d,]+\.\d{2}\)?))?\s*$",
        RegexOptions.Compiled);

    public bool SupportsExtension(string extension)
    {
        return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ParsedStatement> ParseAsync(string filePath, CsvColumnMap? map, CancellationToken cancellationToken = default)
        => ParseText(await ExtractTextAsync(filePath, cancellationToken));

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var document = PdfDocument.Open(filePath);
            var text = new System.Text.StringBuilder();

            foreach (var page in document.GetPages())
            {
                // Group words by height to get lines back; page.Text runs them together.
                foreach (var line in page.GetWords()
                             .GroupBy(w => Math.Round(w.BoundingBox.Bottom))
                             .OrderByDescending(g => g.Key))
                {
                    text.AppendLine(string.Join(' ', line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
                }
            }

            return text.ToString();
        }, cancellationToken);
    }

    public static ParsedStatement ParseText(string text)
    {
        var rows = new List<ParsedRow>();
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var match = Line.Match(lines[i].TrimEnd('\r'));
            if (!match.Success) continue;
            var number = i + 1;
            rows.Add(new ParsedRow(number,
                StatementValues.Date(match.Groups[1].Value, null, number, "date"),
                match.Groups[2].Value,
                StatementValues.Amount(match.Groups[3].Value, number, "amount"),
                // A second trailing number is the running balance.
                match.Groups[4].Success ? StatementValues.Amount(match.Groups[4].Value, number, "balance") : null));
        }
        return new ParsedStatement(rows, true);
    }
}
