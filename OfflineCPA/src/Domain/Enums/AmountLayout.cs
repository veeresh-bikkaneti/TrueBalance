using System.Text.Json.Serialization;

namespace Domain.Enums;

/// <summary>How a bank's CSV says whether money came in or went out.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AmountLayout
{
    /// <summary>One amount column, minus is money out.</summary>
    Signed,
    /// <summary>One amount column, minus is money in.</summary>
    SignedReversed,
    /// <summary>Separate debit and credit columns.</summary>
    DebitCredit,
    /// <summary>One amount column and a column holding the bank's in and out markers.</summary>
    Indicator
}
