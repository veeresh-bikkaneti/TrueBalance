using Application.Interfaces;

namespace TrueBalance.Tests;

public class ApplicationSmokeTests
{
    private sealed class StubStatementImporter : IStatementImporter
    {
        public Task<ParsedStatement> ParseAsync(string filePath, CsvColumnMap? map, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ParsedStatement(Array.Empty<ParsedRow>(), true));
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
    public async Task Statement_importer_contract_returns_rows_and_an_experimental_flag()
    {
        IStatementImporter parser = new StubStatementImporter();

        var parsed = await parser.ParseAsync("local-file.pdf", null);

        Assert.Empty(parsed.Rows);
        Assert.True(parsed.IsExperimental);
    }
}
