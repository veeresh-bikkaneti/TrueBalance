using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public sealed class DataModule : IAdapterModule
{
    public void Register(IServiceCollection services)
    {
        services.AddScoped<ILedgerStore, EfLedgerStore>();
        services.AddScoped<IImportStore, EfImportStore>();
        services.AddScoped<IBookSettingsStore, EfBookSettingsStore>();
        services.AddScoped<IMileageLogStore, EfMileageLogStore>();
    }
}
