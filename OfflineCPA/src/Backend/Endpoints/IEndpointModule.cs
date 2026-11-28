namespace Backend.Endpoints;

/// <summary>
/// One group of routes that signs itself up: drop a class in this assembly and
/// the host finds it. It needs a public parameterless constructor.
/// </summary>
public interface IEndpointModule
{
    /// <summary>Registers what only these routes need. May do nothing.</summary>
    void AddServices(IServiceCollection services);

    void Map(IEndpointRouteBuilder app);
}
