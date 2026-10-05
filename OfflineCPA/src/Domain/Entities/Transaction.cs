namespace Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime Date { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public string? ReferenceDocumentPath { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public ICollection<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
}
