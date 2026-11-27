using System.Text.Json.Serialization;

namespace Domain.Enums;

/// <summary>Whether the statement's running balance agrees with the signed amounts.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BalanceCheck
{
    NotAvailable,
    Passed,
    Failed
}
