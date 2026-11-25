namespace Application.Uploads;

public sealed record StoredUpload(
    Guid Id,
    string SafeFileName,
    string OriginalFileName,
    string Extension,
    long SizeBytes,
    string FullPath);

public sealed class InvalidUploadException : Exception
{
    public InvalidUploadException(string message) : base(message)
    {
    }
}
