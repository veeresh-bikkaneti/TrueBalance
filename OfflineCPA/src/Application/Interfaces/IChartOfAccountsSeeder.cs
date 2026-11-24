using Application.Charts;

namespace Application.Interfaces;

public interface IChartOfAccountsSeeder
{
    IReadOnlyList<string> SupportedKinds { get; }

    Task<ChartSeedFile> LoadAsync(string kind, CancellationToken cancellationToken = default);

    Task<int> SeedAsync(string kind, CancellationToken cancellationToken = default);
}
