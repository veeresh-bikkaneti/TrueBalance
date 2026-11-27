using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Port for a statement file importer. An adapter turns one file's content into
/// rows for staging; it never opens a network connection. Row text is raw:
/// the caller masks it before it is stored or logged.
/// </summary>
public interface IStatementImporter
{
    bool SupportsExtension(string extension);

    /// <exception cref="StatementParseException">The file could not be read as a statement.</exception>
    Task<ParsedStatement> ParseAsync(Stream content, CsvColumnMap? map, CancellationToken cancellationToken = default);
}

/// <summary>One statement line. Amount is signed: money in is positive.</summary>
public sealed record ParsedRow(int RowNumber, DateOnly Date, string Description, decimal Amount, decimal? Balance);

/// <summary>Experimental means the layout was guessed (PDF), so a person has to confirm each row.</summary>
public sealed record ParsedStatement(IReadOnlyList<ParsedRow> Rows, bool IsExperimental)
{
    /// <summary>CSV only: the map used, with its layout filled in.</summary>
    public CsvColumnMap? AppliedMap { get; init; }
}

/// <summary>CSV header names and sign layout. A null layout asks the importer to guess one.</summary>
public sealed record CsvColumnMap(
    string Date, string Description, string? Amount, string? Debit, string? Credit, string? Balance, string? DateFormat,
    AmountLayout? AmountLayout = null, bool DebitsCarryMinus = false, string? IndicatorColumn = null,
    string? MoneyInMarker = null, string? MoneyOutMarker = null);

/// <summary>Carries only a row number and a logical column name, never a value from the file.</summary>
public sealed class StatementParseException : Exception
{
    public StatementParseException(int rowNumber, string column)
        : base($"Row {rowNumber}, column '{column}': the value could not be read.")
    {
    }

    /// <param name="reason">A fixed sentence. Never anything read from the file.</param>
    public StatementParseException(string reason) : base(reason)
    {
    }
}
