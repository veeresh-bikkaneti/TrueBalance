namespace Infrastructure.Pii;

internal static class PiiText
{
    /// <summary>Strips every non-digit character; "123-45-6789" becomes "123456789".</summary>
    public static string OnlyDigits(string value)
    {
        var digits = new char[value.Length];
        var count = 0;
        foreach (var c in value)
        {
            if (char.IsDigit(c))
            {
                digits[count++] = c;
            }
        }

        return new string(digits, 0, count);
    }
}
