using Application.Interfaces;
using Application.Uploads;

namespace Backend.Endpoints;

public sealed record UnlockRequest(string Passphrase);
public sealed record ChangePassphraseRequest(string CurrentPassphrase, string NewPassphrase);
public sealed record RecoveryCodeRequest(string Passphrase);
public sealed record RecoverRequest(string RecoveryCode, string NewPassphrase);
public sealed record PurgeRequest(bool Confirm);

/// <summary>Vault routes. No log line holds a passphrase, a recovery code or a key.</summary>
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

        app.MapPost("/api/vault/change-passphrase", async (ChangePassphraseRequest request, IVaultControl vault, CancellationToken ct) =>
        {
            try
            {
                await vault.ChangePassphraseAsync(request.CurrentPassphrase, request.NewPassphrase, ct);
                return Results.Ok(new { changed = true });
            }
            catch (VaultException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // The only response that ever holds the code. The JSON body keeps other web pages from replacing it.
        app.MapPost("/api/vault/recovery-code", async (RecoveryCodeRequest request, HttpResponse response, IVaultControl vault, CancellationToken ct) =>
        {
            try
            {
                response.Headers.CacheControl = "no-store";
                return Results.Ok(new { recoveryCode = await vault.CreateRecoveryCodeAsync(request.Passphrase, ct) });
            }
            catch (VaultException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/vault/recover", async (RecoverRequest request, IVaultControl vault, CancellationToken ct) =>
        {
            try
            {
                var encrypted = await vault.RecoverAsync(request.RecoveryCode, request.NewPassphrase, ct);
                return Results.Ok(new { unlocked = true, plainFilesEncrypted = encrypted, makeNewCode = true });
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
