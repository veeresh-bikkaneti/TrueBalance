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
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "tax-maps")))
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
            Assert.Contains(lineId.GetString(), lineIds);
        }
    }

    [Theory]
    [MemberData(nameof(MapFiles))]
    public void Map_file_passes_schema_check(string mapFile)
    {
        using var map = JsonDocument.Parse(File.ReadAllText(MapPath(mapFile)));

        Assert.Empty(TaxMapValidator.Validate(map.RootElement));
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

    public static IEnumerable<object[]> MapFiles() => ChartMaps.Select(p => new object[] { p.MapFile });
}
