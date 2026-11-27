using Application.Interfaces;
using Application.Uploads;

namespace Backend.Endpoints;

public sealed record UnlockRequest(string Passphrase);
public sealed record PurgeRequest(bool Confirm);

/// <summary>Vault routes. No response or log line holds the passphrase.</summary>
public static class VaultEndpoints
{
    public static void MapVaultEndpoints(this WebApplication app)
    {
        app.MapPost("/api/vault/unlock", async (UnlockRequest request, IVaultControl vault, CancellationToken ct) =>
        {
            try
            {
                var encrypted = await vault.UnlockAsync(request.Passphrase, ct);
                return Results.Ok(new { unlocked = true, plainFilesEncrypted = encrypted });
            }
            catch (VaultException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/vault/purge-originals", async (PurgeRequest request, PurgeOriginals purge, CancellationToken ct) =>
        {
            try
            {
                var result = await purge.HandleAsync(request.Confirm, ct);
                return result.Purged ? Results.Ok(result) : Results.BadRequest(result);
            }
            catch (VaultException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}
