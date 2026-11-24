using Application.Interfaces;
using Application.Uploads;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class SecureUploadStore : ISecureUploadStore
{
    private const int MaxOriginalNameLength = 200;

    private readonly UploadOptions _options;
    private readonly string _rootFullPath;

    public SecureUploadStore(IOptions<UploadOptions> options)
    {
        _options = options.Value;
        _rootFullPath = Path.GetFullPath(_options.Directory);
        Directory.CreateDirectory(_rootFullPath);
    }

    public async Task<StoredUpload> SaveAsync(
        Stream content,
        long length,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (length <= 0)
        {
            throw new InvalidUploadException("The uploaded file is empty.");
        }

        if (length > _options.MaxBytes)
        {
            throw new InvalidUploadException($"The uploaded file exceeds the {_options.MaxBytes / (1024 * 1024)} MB size limit.");
        }

        var safeOriginalName = SanitizeFileName(originalFileName);
        var extension = Path.GetExtension(safeOriginalName).ToLowerInvariant();

        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new InvalidUploadException($"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", _options.AllowedExtensions)}.");
        }

        if (!string.IsNullOrEmpty(contentType) && !_options.AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new InvalidUploadException($"Content type '{contentType}' is not allowed.");
        }

        var id = Guid.NewGuid();
        var safeFileName = id.ToString("N") + extension;
        var fullPath = Path.GetFullPath(Path.Combine(_rootFullPath, safeFileName));

        if (!fullPath.StartsWith(_rootFullPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidUploadException("The resolved file path escapes the upload directory.");
        }

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
        {
            await content.CopyToAsync(stream, cancellationToken);
        }

        return new StoredUpload(id, safeFileName, safeOriginalName, extension, length, fullPath);
    }

    internal static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\0', '_'));
        name = new string(name.Select(c => char.IsControl(c) ? '_' : c).ToArray());

        if (name.Length > MaxOriginalNameLength)
        {
            name = name[^MaxOriginalNameLength..];
        }

        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }
}
