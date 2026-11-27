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
}
