using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class AdapterModuleRegistration
{
    /// <summary>Finds every <see cref="IAdapterModule"/> in this assembly and lets it register its adapters.</summary>
    public static IServiceCollection AddInfrastructureAdapterModules(this IServiceCollection services)
    {
        foreach (var moduleType in typeof(IAdapterModule).Assembly
                     .GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false }
                                 && typeof(IAdapterModule).IsAssignableFrom(t))
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            var module = (IAdapterModule)Activator.CreateInstance(moduleType)!;
            module.Register(services);
        }

        return services;
    }
}
