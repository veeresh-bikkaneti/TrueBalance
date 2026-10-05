using Domain.Enums;

namespace Domain.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    
    public AccountType Type { get; set; }
    
    public TaxLineItem TaxFormLine { get; set; } = TaxLineItem.None;
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Property
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
