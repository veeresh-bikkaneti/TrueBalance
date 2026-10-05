using Application.Interfaces;
using UglyToad.PdfPig;

namespace Infrastructure.Services;

public class PdfFileParser : ILocalFileParser
{
    public bool SupportsExtension(string extension)
    {
        return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var document = PdfDocument.Open(filePath);
            var text = new System.Text.StringBuilder();

            foreach (var page in document.GetPages())
            {
                text.AppendLine(page.Text);
            }

            return text.ToString();
        }, cancellationToken);
    }
}
