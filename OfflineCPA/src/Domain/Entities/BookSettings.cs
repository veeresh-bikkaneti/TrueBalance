namespace Domain.Entities;

/// <summary>The one record that says which chart the books use and for which tax year.</summary>
public class BookSettings
{
    public const int SingleId = 1;

    public int Id { get; set; } = SingleId;
    public string Chart { get; set; } = string.Empty;
    public int TaxYear { get; set; }
}
