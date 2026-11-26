using Application.Networking;

namespace Infrastructure.Networking;

/// <summary>
/// The shipped integration settings: every integration stays off until the
/// user's own settings (a later prompt) say otherwise. This keeps the app
/// offline by default even if an adapter is installed.
/// </summary>
public sealed class DisabledIntegrationSettings : IIntegrationSettings
{
    public Task<bool> IsEnabledAsync(string adapterName, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
