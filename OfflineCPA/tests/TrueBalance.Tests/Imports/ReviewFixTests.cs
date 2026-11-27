using Application.Interfaces;
using Backend.Services;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace TrueBalance.Tests.Imports;

/// <summary>One test per review finding on the first import commit.</summary>
public sealed class ReviewFixTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tb-fix-" + Guid.NewGuid().ToString("N"));
    private readonly string _input;

    public ReviewFixTests() => Directory.CreateDirectory(_input = Path.Combine(_root, "input"));

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Write(string name, string text)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, text);
        return path;
    }

    private ServiceProvider Services()
    {
        var services = new ServiceCollection();
        var name = Guid.NewGuid().ToString("N");
        services.AddDbContext<OfflineCPADbContext>(o => o.UseInMemoryDatabase(name));
        services.AddOptions();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
            new Infrastructure.Options.UploadOptions { Directory = Path.Combine(_root, "vault") }));
        services.AddInfrastructureAdapterModules();
        services.AddScoped<Application.Imports.QueueImport>();
        return services.BuildServiceProvider();
    }

    private LocalFileProcessorService Watcher(IServiceProvider provider) => new(
        NullLogger<LocalFileProcessorService>.Instance,
        new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Bookkeeping:InputDirectory"] = _input }).Build(),
        provider);

    private void Drop(string text) => File.WriteAllText(Path.Combine(_input, "sample.csv"), text);

    [Fact]
    public async Task A_restart_after_a_failed_move_does_not_queue_the_file_again()
    {
        using var provider = Services();
        // A file where the processed folder should be makes the last move fail.
        File.WriteAllText(Path.Combine(_input, "processed"), "");
        Drop("a");

        await Watcher(provider).ProcessFilesAsync(CancellationToken.None);
        await Watcher(provider).ProcessFilesAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        Assert.Single(scope.ServiceProvider.GetRequiredService<OfflineCPADbContext>().ImportBatches
            .Where(b => b.Extension == ".csv"));
    }

    [Fact]
    public async Task A_second_file_with_the_same_name_keeps_the_first_one()
    {
        using var provider = Services();
        var watcher = Watcher(provider);
        Drop("first");
        await watcher.ProcessFilesAsync(CancellationToken.None);
        Drop("second");
        await watcher.ProcessFilesAsync(CancellationToken.None);

        var kept = Directory.GetFiles(Path.Combine(_input, "processed")).Select(File.ReadAllText).Order();
        Assert.Equal(new[] { "first", "second" }, kept);
    }

    [Fact]
    public async Task Pdf_running_balance_is_not_read_as_the_amount()
    {
        await Task.CompletedTask;
        var row = Assert.Single(PdfFileParser.ParseText("01/05/2026 Fake Vendor 1 -4.50 995.50").Rows);
        Assert.Equal(-4.50m, row.Amount);
        Assert.Equal(995.50m, row.Balance);
        Assert.Equal("Fake Vendor 1", row.Description);
    }

    [Theory]
    // A file with positive debits: a negative one is a reversal, so money comes back.
    [InlineData("2026-06-15,a,30,\n2026-06-16,b,-20.00,\n", new[] { -30.0, 20.0 })]
    // A bank that signs every debit: they are all money out.
    [InlineData("2026-06-15,a,-30,\n2026-06-16,b,(20.00),\n", new[] { -30.0, -20.0 })]
    public async Task Negative_debits_follow_the_file_convention(string body, double[] expected)
    {
        var path = Write("d.csv", "date,description,debit,credit\n" + body);
        var parsed = await new CsvStatementImporter().ParseAsync(path, null);
        Assert.Equal(expected.Select(v => (decimal)v), parsed.Rows.Select(r => r.Amount));
    }

    [Fact]
    public async Task Ofx_with_two_accounts_is_rejected()
    {
        const string One = "<STMTRS><STMTTRN><DTPOSTED>20260615<TRNAMT>1.00</STMTTRN></STMTRS>";
        var path = Write("m.ofx", One + One.Replace("STMTRS", "CCSTMTRS"));
        await Assert.ThrowsAsync<StatementParseException>(() => new OfxStatementImporter().ParseAsync(path, null));
    }
}
