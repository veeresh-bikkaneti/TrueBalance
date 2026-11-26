using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>
/// Adapter registration by convention: each adapter folder has exactly one
/// module class that registers the adapters living in that folder. The host
/// finds every module with reflection, so adding an adapter needs no edits
/// outside its own folder.
/// </summary>
public interface IAdapterModule
{
    void Register(IServiceCollection services);
}
