using Domain.Enums;

namespace Domain.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    
    public AccountType Type { get; set; }
    
    /// <summary>Line in the active tax map, for example SCHC-8. Null means no line.</summary>
    public string? TaxLineId { get; set; }

    /// <summary>Meals are flagged so a CPA can apply the limit in IRS Publication 463.</summary>
    public bool IsMeals { get; set; }
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Property
    public ICollection<Posting> Postings { get; set; } = new List<Posting>();
}
