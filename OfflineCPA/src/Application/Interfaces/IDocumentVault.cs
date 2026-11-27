using Application.Uploads;

namespace Application.Interfaces;

/// <summary>
/// Port for the document vault: an adapter stores one uploaded file under a
/// generated name and never echoes the client's filename back out.
/// </summary>
public interface IDocumentVault
{
    Task<StoredUpload> SaveAsync(
        Stream content,
        long length,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Local path of a stored document, or null when it is not there.</summary>
    string? FindPath(Guid documentId, string extension);
}
