using Infrastructure.Services;

namespace TrueBalance.Tests;

public class InfrastructureSmokeTests
{
    [Fact]
    public void Pdf_parser_supports_only_pdf_extension()
    {
        var parser = new PdfFileParser();

        Assert.True(parser.SupportsExtension(".pdf"));
        Assert.True(parser.SupportsExtension(".PDF"));
        Assert.False(parser.SupportsExtension(".docx"));
        Assert.False(parser.SupportsExtension(".csv"));
    }
}
