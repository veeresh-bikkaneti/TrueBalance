using Application.Interfaces;

namespace TrueBalance.Tests;

public class ApplicationSmokeTests
{
    private sealed class StubStatementImporter : IStatementImporter
    {
        public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"extracted: {filePath}");
        }

        public bool SupportsExtension(string extension)
        {
            return extension.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Statement_importer_contract_supports_pdf()
    {
        IStatementImporter parser = new StubStatementImporter();

        Assert.True(parser.SupportsExtension(".pdf"));
        Assert.True(parser.SupportsExtension(".PDF"));
        Assert.False(parser.SupportsExtension(".txt"));
    }

    [Fact]
    public async Task Statement_importer_contract_extracts_text()
    {
        IStatementImporter parser = new StubStatementImporter();

        var text = await parser.ExtractTextAsync("local-file.pdf");

        Assert.Equal("extracted: local-file.pdf", text);
    }
}
