using Application.Interfaces;
using Application.Uploads;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Vault;

public sealed class VaultModule : IAdapterModule
{
    public void Register(IServiceCollection services)
    {
        // One instance: it holds the key in memory for the life of the process.
        services.AddSingleton<EncryptedFileVault>();
        services.AddSingleton<IDocumentVault>(sp => sp.GetRequiredService<EncryptedFileVault>());
        services.AddSingleton<IVaultControl>(sp => sp.GetRequiredService<EncryptedFileVault>());
        services.AddScoped<PurgeOriginals>();
    }
}
