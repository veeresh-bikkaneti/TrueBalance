using System.Globalization;
using System.Text.RegularExpressions;
using Application.Interfaces;

namespace Infrastructure.Services;

/// <summary>Strict, invariant-culture reading of amounts and dates. When in doubt, reject.</summary>
internal static class StatementValues
{
    public static async Task<string> TextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    // Commas are accepted only as thousands separators, so "1,5" never becomes 15.
    private static readonly Regex Number = new(@"^[+-]?(\d{1,3}(,\d{3})+|\d+)?(\.\d+)?$", RegexOptions.Compiled);
    private static readonly string[] DefaultDateFormats = { "yyyy-MM-dd", "M/d/yyyy" };

    public static decimal Amount(string raw, int row, string column)
    {
        var text = raw.Trim();
        var negative = text.Length > 1 && text[0] == '(' && text[^1] == ')';
        if (negative) text = text[1..^1];
        text = text.Replace("$", string.Empty).Trim();
        if (!text.Any(char.IsAsciiDigit) || !Number.IsMatch(text) || (negative && text[0] is '-' or '+')
            || !decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out var value)
            || value != decimal.Round(value, 2))
        {
            throw new StatementParseException(row, column);
        }
        return negative ? -value : value;
    }

    public static DateOnly Date(string raw, string? format, int row, string column)
        => DateOnly.TryParseExact(raw.Trim(), format is null ? DefaultDateFormats : new[] { format },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new StatementParseException(row, column);
}
