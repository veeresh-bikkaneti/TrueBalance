using Application.Uploads;

namespace Application.Interfaces;

public interface ISecureUploadStore
{
    Task<StoredUpload> SaveAsync(
        Stream content,
        long length,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}
