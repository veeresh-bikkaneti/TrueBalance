namespace Application.Charts;

/// <summary>One chart's lines for one tax year. AccountLines is keyed by four-digit account code.</summary>
public sealed record TaxMap(
    string Chart,
    int TaxYear,
    IReadOnlySet<string> LineIds,
    IReadOnlyDictionary<string, string> AccountLines);
