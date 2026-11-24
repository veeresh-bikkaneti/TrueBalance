using System.Text.RegularExpressions;
using Domain.Enums;
using Infrastructure.Services;

namespace TrueBalance.Tests;

public class ChartSeedTests
{
    private static readonly string[] Kinds = { "household", "sole-proprietor", "s-corporation" };

    private static string SeedPath(string kind)
    {
        return Path.Combine(AppContext.BaseDirectory, "Seed", $"{kind}.json");
    }

    [Theory]
    [InlineData("household")]
    [InlineData("sole-proprietor")]
    [InlineData("s-corporation")]
    public async Task Seed_file_exists_and_matches_its_kind(string kind)
    {
        var seed = await JsonChartOfAccountsSeeder.LoadFileAsync(SeedPath(kind));

        Assert.Equal(kind, seed.Kind);
        Assert.False(string.IsNullOrWhiteSpace(seed.DisplayName));
        Assert.NotEmpty(seed.Accounts);
    }

    [Theory]
    [InlineData("household")]
    [InlineData("sole-proprietor")]
    [InlineData("s-corporation")]
    public async Task Seed_account_codes_are_unique_and_well_formed(string kind)
    {
        var seed = await JsonChartOfAccountsSeeder.LoadFileAsync(SeedPath(kind));

        var codes = seed.Accounts.Select(a => a.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
        Assert.All(codes, code => Assert.Matches(new Regex("^\\d{4}$"), code));
    }

    [Theory]
    [InlineData("household")]
    [InlineData("sole-proprietor")]
    [InlineData("s-corporation")]
    public async Task Seed_account_enums_parse(string kind)
    {
        var seed = await JsonChartOfAccountsSeeder.LoadFileAsync(SeedPath(kind));

        Assert.All(seed.Accounts, account =>
        {
            Assert.True(Enum.TryParse<AccountType>(account.Type, out _), $"Bad type '{account.Type}' on {account.Code}");
            Assert.True(Enum.TryParse<TaxLineItem>(account.TaxFormLine, out _), $"Bad tax line '{account.TaxFormLine}' on {account.Code}");
        });
    }

    [Fact]
    public void Supported_kinds_are_the_three_charts()
    {
        Assert.Equal(Kinds.OrderBy(k => k), new JsonChartOfAccountsSeeder(null!).SupportedKinds.OrderBy(k => k));
    }
}
