using Application.Uploads;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Vault;

public sealed class ThrottleModule : IAdapterModule
{
    public void Register(IServiceCollection services)
    {
        // One instance: every vault route shares one wrong-try count, kept in memory.
        services.AddSingleton<IWrongTryThrottle>(_ => new WrongTryThrottle(TimeProvider.System));
    }
}
