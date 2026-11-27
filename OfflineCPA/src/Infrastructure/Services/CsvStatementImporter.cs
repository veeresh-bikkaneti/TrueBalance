using System.Text;
using Application.Interfaces;

namespace Infrastructure.Services;

/// <summary>Comma-separated statements with a header row and RFC 4180 style quoting.</summary>
public sealed class CsvStatementImporter : IStatementImporter
{
    private static readonly CsvColumnMap Single = new("date", "description", "amount", null, null, "balance", null);

    public bool SupportsExtension(string extension) => extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<ParsedStatement> ParseAsync(string filePath, CsvColumnMap? map, CancellationToken cancellationToken = default)
    {
        var records = Read(await File.ReadAllTextAsync(filePath, cancellationToken));
        if (records.Count == 0) return new ParsedStatement(Array.Empty<ParsedRow>(), false);
        var header = records[0].Select(h => h.Trim()).ToList();

        // Without a saved profile, the plain headers are expected.
        map ??= header.Contains("amount", StringComparer.OrdinalIgnoreCase)
            ? Single
            : Single with { Amount = null, Debit = "debit", Credit = "credit" };

        int Find(string? name, string column)
        {
            var index = name is null ? -1 : header.FindIndex(h => h.Equals(name, StringComparison.OrdinalIgnoreCase));
            return index >= 0 ? index : throw new StatementParseException(1, column);
        }

        var date = Find(map.Date, "date");
        var description = Find(map.Description, "description");
        int? amount = map.Amount is null ? null : Find(map.Amount, "amount");
        int? debit = amount is null ? Find(map.Debit, "debit") : null;
        int? credit = amount is null ? Find(map.Credit, "credit") : null;
        // Balance is optional, and its header may simply not be there.
        var balance = map.Balance is null ? -1 : header.FindIndex(h => h.Equals(map.Balance, StringComparison.OrdinalIgnoreCase));

        var rows = new List<ParsedRow>();
        for (var i = 1; i < records.Count; i++)
        {
            var record = records[i];
            var number = i + 1;
            if (record.All(string.IsNullOrWhiteSpace)) continue;
            string Cell(int index) => index < record.Count ? record[index] : string.Empty;

            decimal value;
            if (amount is { } a)
            {
                value = StatementValues.Amount(Cell(a), number, "amount");
            }
            else
            {
                decimal Side(int index, string column)
                    => string.IsNullOrWhiteSpace(Cell(index)) ? 0m : StatementValues.Amount(Cell(index), number, column);

                // Debit is money out, whether or not the bank writes it with a minus sign.
                var output = Math.Abs(Side(debit!.Value, "debit"));
                var input = Side(credit!.Value, "credit");
                var bothEmpty = string.IsNullOrWhiteSpace(Cell(debit!.Value)) && string.IsNullOrWhiteSpace(Cell(credit!.Value));
                if (bothEmpty || input < 0m || (input != 0m && output != 0m))
                    throw new StatementParseException(number, "credit");
                value = input - output;
            }

            rows.Add(new ParsedRow(number, StatementValues.Date(Cell(date), map.DateFormat, number, "date"),
                Cell(description), value,
                balance < 0 || string.IsNullOrWhiteSpace(Cell(balance))
                    ? null
                    : StatementValues.Amount(Cell(balance), number, "balance")));
        }
        return new ParsedStatement(rows, false);
    }

    private static List<List<string>> Read(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c != '"') field.Append(c);
                else if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = false;
            }
            else if (c == '"' && field.Length == 0) quoted = true;
            else if (c == ',') { record.Add(field.ToString()); field.Clear(); }
            else if (c == '\n')
            {
                record.Add(field.ToString());
                field.Clear();
                records.Add(record);
                record = new List<string>();
            }
            else if (c != '\r') field.Append(c);
        }
        if (quoted) throw new StatementParseException(records.Count + 1, "quote");
        if (field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }
        return records;
    }
}
