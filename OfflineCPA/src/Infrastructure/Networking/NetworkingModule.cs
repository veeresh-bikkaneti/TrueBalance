using Application.Interfaces;
using Application.Ledger;
using Application.Networking;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Networking;

namespace Infrastructure.Networking;

public sealed class NetworkingModule : IAdapterModule
{
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IIntegrationSettings, DisabledIntegrationSettings>();
        services.AddScoped<INetworkAttemptLog, LedgerNetworkAttemptLog>();
        services.AddScoped<INetworkGate, LocalNetworkGate>();
    }
}
