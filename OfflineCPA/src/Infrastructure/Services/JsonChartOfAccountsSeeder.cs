using System.Text.Json;
using Application.Charts;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class JsonChartOfAccountsSeeder : IChartOfAccountsSeeder
{
    private static readonly string[] Kinds = { "household", "sole-proprietor", "s-corporation" };

    // Seed files are camelCase; JsonSerializerDefaults.Web matches them to the PascalCase model.
    private static readonly JsonSerializerOptions SeedSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly OfflineCPADbContext _db;

    private readonly ITaxMapCatalog _maps;

    public JsonChartOfAccountsSeeder(OfflineCPADbContext db, ITaxMapCatalog maps)
    {
        _db = db;
        _maps = maps;
    }

    public IReadOnlyList<string> SupportedKinds => Kinds;

    public async Task<ChartSeedFile> LoadAsync(string kind, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeKind(kind);
        var path = Path.Combine(AppContext.BaseDirectory, "Seed", $"{normalized}.json");
        return await LoadFileAsync(path, cancellationToken);
    }

    public static async Task<ChartSeedFile> LoadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var seed = await JsonSerializer.DeserializeAsync<ChartSeedFile>(stream, SeedSerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Seed file '{path}' is empty.");
        return seed;
    }

    public async Task<int> SeedAsync(string kind, CancellationToken cancellationToken = default)
    {
        return await SeedAsync(await LoadAsync(kind, cancellationToken), cancellationToken);
    }

    public async Task<int> SeedAsync(ChartSeedFile seed, CancellationToken cancellationToken = default)
    {
        var chart = NormalizeKind(seed.Kind);
        var settings = await _db.BookSettings.SingleOrDefaultAsync(cancellationToken);
        // Books keep their first chart and year. A second chart is checked against its own latest map.
        var year = settings?.Chart == chart ? settings.TaxYear : _maps.LatestYear(chart);
        var map = _maps.Find(chart, year);
        var existingCodes = (await _db.Accounts
            .Select(a => a.Code)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var added = 0;
        foreach (var seedAccount in seed.Accounts)
        {
            if (existingCodes.Contains(seedAccount.Code))
            {
                continue;
            }

            if (!Enum.TryParse<AccountType>(seedAccount.Type, out var accountType))
            {
                throw new InvalidOperationException($"Seed account '{seedAccount.Code}' has invalid type '{seedAccount.Type}'.");
            }

            if (seedAccount.TaxLineId is not null && !map.LineIds.Contains(seedAccount.TaxLineId))
            {
                throw new InvalidOperationException($"Seed account '{seedAccount.Code}' has unknown tax line '{seedAccount.TaxLineId}'.");
            }

            _db.Accounts.Add(new Account
            {
                Code = seedAccount.Code,
                Name = seedAccount.Name,
                Type = accountType,
                TaxLineId = seedAccount.TaxLineId,
                IsMeals = seedAccount.IsMeals,
                Description = seedAccount.Description
            });
            existingCodes.Add(seedAccount.Code);
            added++;
        }

        if (settings is null)
        {
            _db.BookSettings.Add(new BookSettings { Chart = chart, TaxYear = year });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return added;
    }

    private static string NormalizeKind(string kind)
    {
        var normalized = kind.Trim().ToLowerInvariant();
        if (!Kinds.Contains(normalized))
        {
            throw new ArgumentException($"Unknown books kind '{kind}'. Supported kinds: {string.Join(", ", Kinds)}.", nameof(kind));
        }

        return normalized;
    }
}
