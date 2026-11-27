using System.Text.Json;
using Infrastructure.TaxMaps;

namespace TrueBalance.Tests;

public class TaxMapTests
{
    private static readonly (string Chart, string MapFile)[] ChartMaps =
    {
        ("sole-proprietor", "schedule-c-2026.json"),
        ("s-corporation", "1120s-2026.json"),
        ("household", "household.json"),
    };

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "OfflineCPA", "src")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("tax-maps folder not found above the test output.");
    }

    private static string MapPath(string file) => Path.Combine(RepoRoot(), "tax-maps", file);

    private static string SeedPath(string chart) => Path.Combine(RepoRoot(), "OfflineCPA", "src", "Infrastructure", "Seed", $"{chart}.json");

    [Theory]
    [MemberData(nameof(ChartMapPairs))]
    public void Every_seeded_account_maps_to_a_valid_line(string chart, string mapFile)
    {
        using var seed = JsonDocument.Parse(File.ReadAllText(SeedPath(chart)));
        using var map = JsonDocument.Parse(File.ReadAllText(MapPath(mapFile)));

        var lineIds = map.RootElement.GetProperty("lines").EnumerateArray()
            .Select(line => line.GetProperty("id").GetString())
            .ToHashSet();
        var accounts = map.RootElement.GetProperty("accounts");

        foreach (var account in seed.RootElement.GetProperty("accounts").EnumerateArray())
        {
            var code = account.GetProperty("code").GetString()!;
            Assert.True(accounts.TryGetProperty(code, out var lineId), $"{chart} account {code} has no entry in {mapFile}.");
            Assert.Equal(lineId.GetString(), account.GetProperty("taxLineId").GetString());
            Assert.Contains(lineId.GetString(), lineIds);
        }
    }

    // CI runs this on its own (--filter Category=TaxMapSchema). It reads every map file in tax-maps/.
    [Fact]
    [Trait("Category", "TaxMapSchema")]
    public void Every_tax_map_file_passes_the_schema_check()
    {
        var directory = Environment.GetEnvironmentVariable("TAX_MAPS_DIR") ?? Path.Combine(RepoRoot(), "tax-maps");
        var files = Directory.GetFiles(directory, "*.json");

        Assert.NotEmpty(files);
        Assert.All(files, file =>
        {
            using var map = JsonDocument.Parse(File.ReadAllText(file));
            var errors = TaxMapValidator.Validate(map.RootElement);
            Assert.True(errors.Count == 0, $"{Path.GetFileName(file)}: {string.Join(" ", errors)}");
        });
    }

    [Fact]
    public void Schema_check_fails_on_broken_map()
    {
        const string broken = """
        {
          "form": "Schedule C (Form 1040)",
          "taxYear": 2026,
          "chart": "sole-proprietor",
          "sources": [],
          "lines": [ { "id": "SCHC-1", "label": "Gross receipts", "confidence": "Moderate 1/1" } ],
          "accounts": { "4000": "SCHC-99" }
        }
        """;
        using var map = JsonDocument.Parse(broken);

        var errors = TaxMapValidator.Validate(map.RootElement);

        Assert.Contains(errors, e => e.Contains("sources"));
        Assert.Contains(errors, e => e.Contains("SCHC-99"));
    }

    public static IEnumerable<object[]> ChartMapPairs() => ChartMaps.Select(p => new object[] { p.Chart, p.MapFile });
}
