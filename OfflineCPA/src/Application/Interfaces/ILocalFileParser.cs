namespace Application.Interfaces;

public interface ILocalFileParser
{
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
    bool SupportsExtension(string extension);
}
