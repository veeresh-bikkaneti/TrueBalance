using Application.Imports;
using Application.Interfaces;
using Application.Uploads;

namespace Backend.Endpoints;

/// <summary>
/// The upload route stores the file and queues it for import. Nothing is parsed
/// or posted here, and neither the client file name nor the stored path reaches
/// the database (ADR 0010).
/// </summary>
public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/api/upload", async (
            IFormFile file,
            IDocumentVault vault,
            IEnumerable<IStatementImporter> importers,
            IPiiMasker masker,
            QueueImport queue,
            CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "No file uploaded." });
            }

            StoredUpload stored;
            try
            {
                stored = await vault.SaveAsync(
                    file.OpenReadStream(),
                    file.Length,
                    file.FileName,
                    file.ContentType,
                    cancellationToken);
            }
            catch (VaultLockedException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidUploadException ex)
            {
                // Upload messages quote parts of the input, so they pass the
                // masker before they reach a response body.
                return Results.BadRequest(new { error = masker.Mask(ex.Message) });
            }

            if (importers.Any(i => i.SupportsExtension(stored.Extension)))
            {
                var batchId = await queue.HandleAsync(stored.Id, stored.Extension, cancellationToken);
                return Results.Ok(new { message = "File stored locally and queued for import.", id = stored.Id, importBatchId = batchId });
            }

            return Results.Ok(new { message = "File uploaded securely to local user-assets.", id = stored.Id });
        })
        .WithName("UploadFile")
        .DisableAntiforgery();
    }
}
