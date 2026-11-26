using Application.Interfaces;
using Application.Uploads;
using Domain.Entities;
using Infrastructure.Data;

namespace Backend.Endpoints;

/// <summary>
/// The upload route. The transaction stores only the vault-generated id:
/// the client file name is masked before it reaches the description, and
/// the stored path never reaches the database (ADR 0010).
/// </summary>
public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/api/upload", async (
            IFormFile file,
            IDocumentVault vault,
            IStatementImporter importer,
            IPiiMasker masker,
            OfflineCPADbContext db,
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
            catch (InvalidUploadException ex)
            {
                // Upload messages quote parts of the input, so they pass the
                // masker before they reach a response body.
                return Results.BadRequest(new { error = masker.Mask(ex.Message) });
            }

            if (importer.SupportsExtension(stored.Extension))
            {
                try
                {
                    await importer.ExtractTextAsync(stored.FullPath, cancellationToken);

                    var transaction = new Transaction
                    {
                        Description = $"Uploaded file {stored.Id} ({masker.Mask(stored.OriginalFileName)})",
                        Date = DateTime.UtcNow,
                        ReferenceDocumentPath = stored.Id.ToString()
                    };

                    db.Transactions.Add(transaction);
                    await db.SaveChangesAsync(cancellationToken);
                    return Results.Ok(new { message = "File uploaded and processed smoothly locally.", id = stored.Id });
                }
                catch (Exception)
                {
                    return Results.Problem("File saved locally, but text extraction failed.");
                }
            }

            return Results.Ok(new { message = "File uploaded securely to local user-assets.", id = stored.Id });
        })
        .WithName("UploadFile")
        .DisableAntiforgery();
    }
}
