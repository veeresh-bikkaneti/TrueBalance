using Application.Charts;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Migrations;
using Infrastructure.Services;
using Infrastructure.TaxMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace TrueBalance.Tests;

public class TaxLineTests
{
    private static string MapsDirectory => Path.Combine(AppContext.BaseDirectory, "tax-maps");

    private static OfflineCPADbContext NewDb() => new(new DbContextOptionsBuilder<OfflineCPADbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Theory]
    [InlineData("household")]
    [InlineData("sole-proprietor")]
    [InlineData("s-corporation")]
    public async Task Seeding_stores_the_mapped_line_and_the_book_settings(string chart)
    {
        using var db = NewDb();
        var catalog = new JsonTaxMapCatalog(MapsDirectory);

        await new JsonChartOfAccountsSeeder(db, catalog).SeedAsync(chart);

        var map = catalog.Find(chart, 2026);
        Assert.All(db.Accounts.ToList(), a => Assert.Equal(map.AccountLines[a.Code], a.TaxLineId));
        var settings = Assert.Single(db.BookSettings.ToList());
        Assert.Equal((chart, 2026), (settings.Chart, settings.TaxYear));
    }

    [Fact]
    public async Task Seeder_refuses_an_unknown_line_id()
    {
        using var db = NewDb();
        var seed = new ChartSeedFile
        {
            Kind = "s-corporation",
            Accounts = { new SeedAccount { Code = "9999", Name = "Test", Type = "Expense", TaxLineId = "1120S-99" } }
        };
        var seeder = new JsonChartOfAccountsSeeder(db, new JsonTaxMapCatalog(MapsDirectory));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync(seed));

        Assert.Contains("1120S-99", error.Message);
        Assert.Empty(db.Accounts.ToList());
    }

    [Fact]
    public async Task Changing_the_tax_year_swaps_maps_without_a_code_change()
    {
        var directory = Directory.CreateTempSubdirectory("tb-maps-").FullName;
        foreach (var (year, line) in new[] { (2026, "OLD-1"), (2027, "NEW-1") })
        {
            File.WriteAllText(Path.Combine(directory, $"x-{year}.json"), $$$"""
                {"form":"F","chart":"s-corporation","taxYear":{{{year}}},"sources":["s"],
                 "lines":[{"id":"{{{line}}}","label":"L","confidence":"[Inferred]"}],"accounts":{"4000":"{{{line}}}"}}
                """);
        }

        using var db = NewDb();
        var settings = new BookSettings { Chart = "s-corporation", TaxYear = 2026 };
        db.BookSettings.Add(settings);
        await db.SaveChangesAsync();
        var activeMap = new GetActiveTaxMap(new EfBookSettingsStore(db), new JsonTaxMapCatalog(directory));

        Assert.Equal("OLD-1", (await activeMap.ExecuteAsync())!.AccountLines["4000"]);
        settings.TaxYear = 2027;
        await db.SaveChangesAsync();
        Assert.Equal("NEW-1", (await activeMap.ExecuteAsync())!.AccountLines["4000"]);
    }

    [Fact]
    public void The_1099_threshold_is_read_from_year_data()
    {
        Assert.Equal(2000m, new JsonTaxMapCatalog(MapsDirectory).Form1099Threshold(2026));

        var directory = Directory.CreateTempSubdirectory("tb-years-").FullName;
        Directory.CreateDirectory(Path.Combine(directory, "years"));
        File.WriteAllText(Path.Combine(directory, "years", "2030.json"), """{"taxYear":2030,"form1099Threshold":1234}""");
        Assert.Equal(1234m, new JsonTaxMapCatalog(directory).Form1099Threshold(2030));
    }

    // Numbers of the removed enum, as the old TaxFormLine column stored them.
    [Theory]
    [InlineData(1, "1120S-1A")]
    [InlineData(2, "1120S-2")]
    [InlineData(7, "1120S-7")]
    [InlineData(8, "1120S-8")]
    [InlineData(9, "1120S-9")]
    [InlineData(10, "1120S-10")]
    [InlineData(11, "1120S-11")]
    [InlineData(12, "1120S-12")]
    [InlineData(13, "1120S-13")]
    [InlineData(14, "1120S-14")]
    [InlineData(16, "1120S-16")]
    [InlineData(17, "1120S-17")]
    [InlineData(18, "1120S-18")]
    [InlineData(19, "1120S-20")]
    [InlineData(20, "1120S-20")]
    [InlineData(21, "1120S-20")]
    [InlineData(101, "SCHL-1")]
    [InlineData(102, "SCHL-2A")]
    [InlineData(116, "SCHL-16")]
    [InlineData(120, "SCHL-20")]
    [InlineData(122, "SCHL-22")]
    [InlineData(124, "SCHL-24")]
    public void Migration_maps_each_old_value(int oldValue, string lineId)
    {
        Assert.Equal(lineId, TaxLineIds.LineIdByOldValue[oldValue]);
    }

    [Fact]
    public void Migration_maps_every_value_to_a_real_line_and_leaves_none_null()
    {
        var map = new JsonTaxMapCatalog(MapsDirectory).Find("s-corporation", 2026);

        Assert.All(TaxLineIds.LineIdByOldValue.Values, id => Assert.Contains(id, map.LineIds));
        Assert.False(TaxLineIds.LineIdByOldValue.ContainsKey(0));
        Assert.Equal(37, TaxLineIds.LineIdByOldValue.Count);
    }

    [Fact]
    public void Migration_copies_values_before_dropping_the_old_column()
    {
        var operations = new TaxLineIds().UpOperations.ToList();

        var add = operations.FindIndex(o => o is AddColumnOperation { Name: "TaxLineId" });
        var copy = operations.FindIndex(o => o is SqlOperation s && s.Sql.Contains("WHEN 21 THEN '1120S-20'"));
        var drop = operations.FindIndex(o => o is DropColumnOperation { Name: "TaxFormLine" });
        Assert.True(add >= 0 && add < copy && copy < drop, $"add {add}, copy {copy}, drop {drop}");
        Assert.Contains(new TaxLineIds().DownOperations, o => o is AddColumnOperation { Name: "TaxFormLine" });
    }

    [Theory]
    [InlineData(10, 100, 120, true)]
    [InlineData(0, 100, 120, false)]
    [InlineData(30, 100, 120, false)]
    [InlineData(10, 120, 100, false)]
    public async Task Mileage_log_stores_only_consistent_entries(int miles, int start, int end, bool valid)
    {
        using var db = NewDb();
        IMileageLogStore store = new EfMileageLogStore(db);
        MileageLogEntry Create() => MileageLogEntry.Create(new DateOnly(2026, 3, 1), "Client visit", miles, start, end);

        if (!valid)
        {
            Assert.Throws<ArgumentException>(() => Create());
            return;
        }

        await store.AddAsync(Create());
        Assert.Equal(miles, Assert.Single(await store.ListAsync()).Miles);
    }
}
