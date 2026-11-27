using Domain.Enums;

namespace Domain.Entities;

/// <summary>Which CSV header holds which field, saved once per bank.</summary>
public class CsvMappingProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BankName { get; set; } = string.Empty;
    public string DateColumn { get; set; } = string.Empty;
    public string DescriptionColumn { get; set; } = string.Empty;
    public string? AmountColumn { get; set; }
    public string? DebitColumn { get; set; }
    public string? CreditColumn { get; set; }
    public string? BalanceColumn { get; set; }
    public string? DateFormat { get; set; }
    public AmountLayout AmountLayout { get; set; }
    /// <summary>DebitCredit only: the bank writes debits as negative numbers.</summary>
    public bool DebitsCarryMinus { get; set; }
    public string? IndicatorColumn { get; set; }
    public string? MoneyInMarker { get; set; }
    public string? MoneyOutMarker { get; set; }
    /// <summary>A person confirmed the layout. The file alone can never set this.</summary>
    public bool LayoutConfirmed { get; set; }
}
