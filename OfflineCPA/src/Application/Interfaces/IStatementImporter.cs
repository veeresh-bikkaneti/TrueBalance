namespace Application.Interfaces;

/// <summary>
/// Port for a statement file importer. An adapter turns one local file into
/// text for the import pipeline; it never opens a network connection.
/// </summary>
public interface IStatementImporter
{
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
    bool SupportsExtension(string extension);
}
