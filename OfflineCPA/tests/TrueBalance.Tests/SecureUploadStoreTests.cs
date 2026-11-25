using System.Text;
using Application.Uploads;
using Infrastructure.Options;
using Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace TrueBalance.Tests;

public class SecureUploadStoreTests : IDisposable
{
    private readonly string _root;

    public SecureUploadStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "tb-uploads-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private SecureUploadStore CreateStore(long maxBytes = 20 * 1024 * 1024)
    {
        return new SecureUploadStore(Options.Create(new UploadOptions { Directory = _root, MaxBytes = maxBytes }));
    }

    private static MemoryStream Content(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    [Theory]
    [InlineData("../../x.csv")]
    [InlineData("..\\x.csv")]
    [InlineData("../../evil.csv")]
    [InlineData("..\\evil.csv")]
    [InlineData("/etc/evil.csv")]
    [InlineData("C:\\temp\\evil.csv")]
    [InlineData("evil\0.csv")]
    public async Task Hostile_names_save_inside_root_under_a_generated_name(string hostileName)
    {
        var store = CreateStore();

        var stored = await store.SaveAsync(Content("a,b\n1,2\n"), 8, hostileName, "text/csv");

        var rootFullPath = Path.GetFullPath(_root);
        Assert.StartsWith(rootFullPath + Path.DirectorySeparatorChar, stored.FullPath, StringComparison.Ordinal);
        Assert.EndsWith(".csv", stored.SafeFileName, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(hostileName, Path.GetFileName(stored.FullPath));
        Assert.True(File.Exists(stored.FullPath));
    }

    [Fact]
    public async Task Valid_upload_keeps_original_name_as_data_only()
    {
        var store = CreateStore();

        var stored = await store.SaveAsync(Content("a,b\n1,2\n"), 8, "statement.csv", "text/csv");

        Assert.Equal("statement.csv", stored.OriginalFileName);
        Assert.NotEqual("statement.csv", stored.SafeFileName);
        Assert.StartsWith(Path.GetFullPath(_root), stored.FullPath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Disallowed_extension_is_rejected()
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<InvalidUploadException>(
            () => store.SaveAsync(Content("MZ"), 2, "program.exe", "application/octet-stream"));
    }

    [Fact]
    public async Task Oversize_file_is_rejected()
    {
        var store = CreateStore(maxBytes: 4);

        await Assert.ThrowsAsync<InvalidUploadException>(
            () => store.SaveAsync(Content("this is longer than four bytes"), 30, "big.csv", "text/csv"));
    }

    [Fact]
    public async Task Empty_file_is_rejected()
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<InvalidUploadException>(
            () => store.SaveAsync(Content(""), 0, "empty.csv", "text/csv"));
    }

    [Fact]
    public async Task Disallowed_content_type_is_rejected()
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<InvalidUploadException>(
            () => store.SaveAsync(Content("a,b\n1,2\n"), 8, "statement.csv", "text/html"));
    }
}
