using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

public sealed class ServicesModule : IAdapterModule
{
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IStatementImporter, PdfFileParser>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IChartOfAccountsSeeder, JsonChartOfAccountsSeeder>();
        services.AddScoped<IDocumentVault, SecureUploadStore>();
    }
}
