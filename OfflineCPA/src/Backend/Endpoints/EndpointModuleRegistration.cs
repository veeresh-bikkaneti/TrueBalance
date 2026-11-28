using System.Reflection;

namespace Backend.Endpoints;

public static class EndpointModuleRegistration
{
    /// <summary>Lets every <see cref="IEndpointModule"/> register its services. Defaults to this assembly.</summary>
    public static IServiceCollection AddEndpointModules(this IServiceCollection services, Assembly? assembly = null)
    {
        foreach (var module in Find(assembly))
        {
            module.AddServices(services);
        }

        return services;
    }

    /// <summary>Lets every <see cref="IEndpointModule"/> map its routes. Defaults to this assembly.</summary>
    public static IEndpointRouteBuilder MapEndpointModules(this IEndpointRouteBuilder app, Assembly? assembly = null)
    {
        foreach (var module in Find(assembly))
        {
            module.Map(app);
        }

        return app;
    }

    private static IEnumerable<IEndpointModule> Find(Assembly? assembly) =>
        (assembly ?? typeof(IEndpointModule).Assembly)
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpointModule).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .Select(t => (IEndpointModule)Activator.CreateInstance(t)!);
}
