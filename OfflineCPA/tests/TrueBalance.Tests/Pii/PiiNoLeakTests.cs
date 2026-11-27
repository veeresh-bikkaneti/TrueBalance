using System.Net.Http.Json;
using System.Net.Http.Headers;
using Backend.Endpoints;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

/// <summary>
/// The ADR 0010 no-leak gate: fake SSN, EIN, ITIN, routing and account
/// numbers pass through the masker and the stub importer path, and none of
/// the raw values may appear in any database text column, in captured logs,
/// or in an error body. Statement import (prompt 05) cannot merge until
/// this passes.
/// </summary>
public class PiiNoLeakTests
{
    private static (WebApplication App, OfflineCPADbContext Db, CapturingLoggerProvider Logs) BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var masker = new MaskingPiiMasker();

        builder.Services.AddSingleton<Application.Interfaces.IDocumentVault>(new FakeVault());
        builder.Services.AddSingleton<Application.Interfaces.IStatementImporter, Infrastructure.Services.CsvStatementImporter>();
        builder.Services.AddSingleton<Application.Interfaces.IStatementImporter, Infrastructure.Services.OfxStatementImporter>();
        builder.Services.AddSingleton<Application.Interfaces.IStatementImporter, Infrastructure.Services.PdfFileParser>();
        builder.Services.AddSingleton<Application.Interfaces.IPiiMasker>(masker);
        var databaseName = $"pii-no-leak-{Guid.NewGuid():N}";
        builder.Services.AddDbContext<OfflineCPADbContext>(options => options.UseInMemoryDatabase(databaseName));

        var logs = new CapturingLoggerProvider();
        builder.Logging.ClearProviders();
        builder.Logging.Services.AddSingleton<ILoggerProvider>(
            _ => new Backend.Extensions.PiiRedactionLoggerProvider(logs, masker));

        builder.Services.AddScoped<Application.Interfaces.ILedgerStore, Infrastructure.Data.EfLedgerStore>();
        builder.Services.AddSingleton<Application.Interfaces.IClock, Infrastructure.Services.SystemClock>();
        builder.Services.AddScoped<Application.Ledger.CreateDraftEntry>();
        builder.Services.AddScoped<Application.Ledger.PostEntry>();
        builder.Services.AddScoped<Application.Ledger.ReverseEntry>();
        builder.Services.AddScoped<Application.Ledger.LockPeriod>();
        builder.Services.AddScoped<Application.Ledger.GetAccountBalances>();

        builder.Services.AddScoped<Application.Interfaces.IImportStore, Infrastructure.Data.EfImportStore>();
        builder.Services.AddScoped<Application.Imports.QueueImport>();

        var app = builder.Build();
        app.MapUploadEndpoints();
        app.MapLedgerEndpoints();

        var db = app.Services.CreateScope().ServiceProvider.GetRequiredService<OfflineCPADbContext>();
        db.Database.EnsureCreated();

        return (app, db, logs);
    }

    private static MultipartFormDataContent UploadContent(string fileName, string contentType)
    {
        var file = new ByteArrayContent([1, 2, 3, 4]);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent
        {
            { file, "file", fileName },
        };
    }

    private static async Task<string> PostUploadAsync(WebApplication app, MultipartFormDataContent content)
    {
        using var client = app.GetTestClient();
        var response = await client.PostAsync("/api/upload", content);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Raw_values_never_reach_the_database_text_columns()
    {
        var (app, db, _) = BuildApp();
        await app.StartAsync();
        try
        {
            // The stub importer path: extracted text goes through the masker
            // before it is persisted, the way prompt 05 will use the port.
            var masker = app.Services.GetRequiredService<Application.Interfaces.IPiiMasker>();
            var text = await new StubImporter(FakePii.SampleStatementText()).ExtractTextAsync("ignored.pdf");
            db.Transactions.Add(new Transaction
            {
                Description = masker.Mask(text),
                Date = DateTime.UtcNow,
            });
            db.Transactions.Add(new Transaction
            {
                Description = $"Import of file {Guid.NewGuid()}",
                Date = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            Assert.All(db.ChangeTracker.Entries(), entry =>
                Assert.All(
                    entry.Properties.Where(p => p.Metadata.ClrType == typeof(string) && p.CurrentValue is not null),
                    p =>
                    {
                        var value = (string)p.CurrentValue!;
                        foreach (var raw in FakePii.LeakTargets())
                        {
                            Assert.DoesNotContain(raw, value, StringComparison.Ordinal);
                        }
                    }));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Upload_response_and_transaction_avoid_raw_values()
    {
        var (app, db, _) = BuildApp();
        await app.StartAsync();
        try
        {
            var body = await PostUploadAsync(
                app,
                UploadContent($"statement for {FakePii.SsnFormatted} {FakePii.AccountNumberA}.pdf", "application/pdf"));

            foreach (var raw in FakePii.LeakTargets())
            {
                Assert.DoesNotContain(raw, body, StringComparison.Ordinal);
            }

            // The upload only queues: no transaction, and the batch holds no file name.
            Assert.Empty(db.Transactions);
            var batch = Assert.Single(db.ImportBatches);
            Assert.Equal(".pdf", batch.Extension);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Error_bodies_never_echo_raw_input_values()
    {
        var (app, _, _) = BuildApp();
        await app.StartAsync();
        try
        {
            var body = await PostUploadAsync(
                app,
                UploadContent($"{FakePii.SsnFormatted}.pdf", "application/x-invalid"));

            Assert.Contains("Content rejected", body, StringComparison.Ordinal);
            foreach (var raw in FakePii.LeakTargets())
            {
                Assert.DoesNotContain(raw, body, StringComparison.Ordinal);
            }
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Ledger_error_bodies_never_echo_raw_values()
    {
        var (app, db, _) = BuildApp();
        var period = Domain.Entities.Period.Create(
            $"Q4 {FakePii.AccountNumberA}", new DateOnly(2024, 10, 1), new DateOnly(2024, 12, 31));
        period.Lock();
        period.PullEvents();
        db.Add(period);
        db.SaveChanges();
        await app.StartAsync();
        try
        {
            var response = await app.GetTestClient().PostAsJsonAsync("/api/ledger/entries", new CreateEntryRequest(
                new DateOnly(2024, 11, 25), "memo", period.Id,
                [new(Guid.NewGuid(), 5m, null), new(Guid.NewGuid(), -5m, null)]));
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("is locked", body, StringComparison.Ordinal);
            Assert.DoesNotContain(FakePii.AccountNumberA, body, StringComparison.Ordinal);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Raw_values_never_reach_captured_logs()
    {
        var (app, _, logs) = BuildApp();
        await app.StartAsync();
        try
        {
            var logger = app.Services.GetRequiredService<ILogger<PiiNoLeakTests>>();
            logger.LogInformation(
                "Imported statement text: {Text}",
                FakePii.SampleStatementText());

            Assert.NotEmpty(logs.Lines);
            foreach (var line in logs.Lines)
            {
                foreach (var raw in FakePii.LeakTargets())
                {
                    Assert.DoesNotContain(raw, line, StringComparison.Ordinal);
                }
            }
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
