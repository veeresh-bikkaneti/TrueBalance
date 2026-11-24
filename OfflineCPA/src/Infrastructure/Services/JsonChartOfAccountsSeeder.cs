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

    private readonly OfflineCPADbContext _db;

    public JsonChartOfAccountsSeeder(OfflineCPADbContext db)
    {
        _db = db;
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
        var seed = await JsonSerializer.DeserializeAsync<ChartSeedFile>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException($"Seed file '{path}' is empty.");
        return seed;
    }

    public async Task<int> SeedAsync(string kind, CancellationToken cancellationToken = default)
    {
        var seed = await LoadAsync(kind, cancellationToken);
        var existingCodes = await _db.Accounts
            .Select(a => a.Code)
            .ToHashSetAsync(cancellationToken);

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

            if (!Enum.TryParse<TaxLineItem>(seedAccount.TaxFormLine, out var taxLine))
            {
                throw new InvalidOperationException($"Seed account '{seedAccount.Code}' has invalid tax form line '{seedAccount.TaxFormLine}'.");
            }

            _db.Accounts.Add(new Account
            {
                Code = seedAccount.Code,
                Name = seedAccount.Name,
                Type = accountType,
                TaxFormLine = taxLine,
                Description = seedAccount.Description
            });
            existingCodes.Add(seedAccount.Code);
            added++;
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
