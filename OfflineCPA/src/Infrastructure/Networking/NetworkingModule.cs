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
        // The scoped decorator gives each gate row its own unit of work, so a
        // gate call cannot commit a use case's pending changes.
        services.AddScoped<INetworkAttemptLog, ScopedNetworkAttemptLog>();
        services.AddScoped<INetworkGate, LocalNetworkGate>();
    }
}
