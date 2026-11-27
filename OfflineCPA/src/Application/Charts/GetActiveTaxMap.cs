using Application.Interfaces;

namespace Application.Charts;

/// <summary>The map for the chart and year in the book settings, or null before setup.</summary>
public sealed class GetActiveTaxMap(IBookSettingsStore settings, ITaxMapCatalog maps)
{
    public async Task<TaxMap?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var current = await settings.GetAsync(cancellationToken);
        return current is null ? null : maps.Find(current.Chart, current.TaxYear);
    }
}
