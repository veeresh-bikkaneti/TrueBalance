using System.Globalization;
using Application.Interfaces;
using Application.Uploads;
using Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Backend.Endpoints;

public sealed record UnlockRequest(string Passphrase);
public sealed record ChangePassphraseRequest(string CurrentPassphrase, string NewPassphrase);
public sealed record RecoveryCodeRequest(string Passphrase);
public sealed record RecoverRequest(string RecoveryCode, string NewPassphrase);
public sealed record PurgeRequest(bool Confirm);

/// <summary>Vault routes. No log line holds a passphrase, a recovery code or a key.</summary>
public static class VaultEndpoints
{
    // The vault's one answer to a wrong passphrase or code. A test with the real vault keeps the two in step.
    private const string WrongSecret = "That passphrase does not open the books.";
    private const string MustWait = "Too many tries. Wait a moment, then try again.";

    public static void MapVaultEndpoints(this WebApplication app)
    {
        // [FromServices]: resolved when asked, so a host that never asks needs no document vault.
        app.MapGet("/api/vault/status", ([FromServices] IDocumentVault vault, [FromServices] IOptions<UploadOptions> uploads) =>
            Results.Ok(new
            {
                unlocked = vault.IsUnlocked,
                passphraseSet = File.Exists(Path.Combine(uploads.Value.Directory, "vault.json")),
            }));

        app.MapPost("/api/vault/unlock", (UnlockRequest request, HttpResponse response, IVaultControl vault, IWrongTryThrottle throttle, CancellationToken ct) =>
            CheckSecretAsync(throttle, response, async () =>
            {
                var encrypted = await vault.UnlockAsync(request.Passphrase, ct);
                return new { unlocked = true, plainFilesEncrypted = encrypted };
            }));

        app.MapPost("/api/vault/change-passphrase", (ChangePassphraseRequest request, HttpResponse response, IVaultControl vault, IWrongTryThrottle throttle, CancellationToken ct) =>
            CheckSecretAsync(throttle, response, async () =>
            {
                await vault.ChangePassphraseAsync(request.CurrentPassphrase, request.NewPassphrase, ct);
                return new { changed = true };
            }));

        // The only response that ever holds the code. The JSON body keeps other web pages from replacing it.
        app.MapPost("/api/vault/recovery-code", (RecoveryCodeRequest request, HttpResponse response, IVaultControl vault, IWrongTryThrottle throttle, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            return CheckSecretAsync(throttle, response, async () =>
                new { recoveryCode = await vault.CreateRecoveryCodeAsync(request.Passphrase, ct) });
        });

        app.MapPost("/api/vault/recover", (RecoverRequest request, HttpResponse response, IVaultControl vault, IWrongTryThrottle throttle, CancellationToken ct) =>
            CheckSecretAsync(throttle, response, async () =>
            {
                var encrypted = await vault.RecoverAsync(request.RecoveryCode, request.NewPassphrase, ct);
                return new { unlocked = true, plainFilesEncrypted = encrypted, makeNewCode = true };
            }));

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

    /// <summary>Runs one check of a secret, unless wrong answers say to wait. Waiting never touches the vault.</summary>
    private static async Task<IResult> CheckSecretAsync<T>(IWrongTryThrottle throttle, HttpResponse response, Func<Task<T>> check)
    {
        using var attempt = throttle.TryBegin(out var retryAfter);
        if (attempt is null)
        {
            response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            return Results.Json(new { error = MustWait }, statusCode: StatusCodes.Status429TooManyRequests);
        }

        try
        {
            var body = await check();
            attempt.Right();
            return Results.Ok(body);
        }
        catch (VaultException ex)
        {
            // Other refusals, such as a short new passphrase, neither count nor reset.
            if (ex.Message == WrongSecret)
            {
                attempt.Wrong();
            }

            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
