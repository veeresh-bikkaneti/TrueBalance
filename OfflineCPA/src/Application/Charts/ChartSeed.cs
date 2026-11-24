namespace Application.Charts;

public sealed class ChartSeedFile
{
    public string Kind { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<SeedAccount> Accounts { get; set; } = new();
}

public sealed class SeedAccount
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TaxFormLine { get; set; } = "None";
    public string? Description { get; set; }
}
