using System.Text.Json;
using Application.Charts;
using Application.Interfaces;

namespace Infrastructure.TaxMaps;

/// <summary>
/// Reads tax maps from a folder: one map per *.json file, and yearly figures in years/{year}.json.
/// A new year is a new file, with no code change.
/// </summary>
public sealed class JsonTaxMapCatalog(string directory) : ITaxMapCatalog
{
    public TaxMap Find(string chart, int taxYear) =>
        Load().SingleOrDefault(m => m.Chart == chart && m.TaxYear == taxYear)
        ?? throw new InvalidOperationException($"No tax map for chart '{chart}' and tax year {taxYear}.");

    public int LatestYear(string chart) =>
        Load().Where(m => m.Chart == chart).Select(m => (int?)m.TaxYear).Max()
        ?? throw new InvalidOperationException($"No tax map for chart '{chart}'.");

    public decimal Form1099Threshold(int taxYear)
    {
        var path = Path.Combine(directory, "years", $"{taxYear}.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"No yearly tax figures for {taxYear}.");
        }

        using var year = JsonDocument.Parse(File.ReadAllText(path));
        return year.RootElement.GetProperty("form1099Threshold").GetDecimal();
    }

    private IEnumerable<TaxMap> Load()
    {
        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var root = document.RootElement;
            var errors = TaxMapValidator.Validate(root);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Tax map '{Path.GetFileName(file)}' is broken: {string.Join(" ", errors)}");
            }

            yield return new TaxMap(
                root.GetProperty("chart").GetString()!,
                root.GetProperty("taxYear").GetInt32(),
                root.GetProperty("lines").EnumerateArray().Select(l => l.GetProperty("id").GetString()!).ToHashSet(),
                root.GetProperty("accounts").EnumerateObject().ToDictionary(a => a.Name, a => a.Value.GetString()!));
        }
    }
}
