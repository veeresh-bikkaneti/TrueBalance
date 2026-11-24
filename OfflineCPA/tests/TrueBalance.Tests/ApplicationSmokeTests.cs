using Application.Interfaces;

namespace TrueBalance.Tests;

public class ApplicationSmokeTests
{
    private sealed class StubLocalFileParser : ILocalFileParser
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
    public void Local_file_parser_contract_supports_pdf()
    {
        ILocalFileParser parser = new StubLocalFileParser();

        Assert.True(parser.SupportsExtension(".pdf"));
        Assert.True(parser.SupportsExtension(".PDF"));
        Assert.False(parser.SupportsExtension(".txt"));
    }

    [Fact]
    public async Task Local_file_parser_contract_extracts_text()
    {
        ILocalFileParser parser = new StubLocalFileParser();

        var text = await parser.ExtractTextAsync("local-file.pdf");

        Assert.Equal("extracted: local-file.pdf", text);
    }
}
