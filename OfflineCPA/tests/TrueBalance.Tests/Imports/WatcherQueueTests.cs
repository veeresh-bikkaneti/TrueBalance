using Application.Interfaces;
using Backend.Services;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace TrueBalance.Tests.Imports;

public class WatcherQueueTests
{
    [Fact]
    public async Task Watcher_queues_a_batch_once_and_writes_no_transaction_or_rows()
    {
        var root = Path.Combine(Path.GetTempPath(), "tb-queue-" + Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "input");
        Directory.CreateDirectory(input);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(input, "sample.csv"), "date,description,amount\n2026-06-15,Fake Vendor 1,-1.00\n");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Bookkeeping:InputDirectory"] = input })
                .Build();
            var services = new ServiceCollection();
            var name = Guid.NewGuid().ToString("N");
            services.AddDbContext<OfflineCPADbContext>(o => o.UseInMemoryDatabase(name));
            services.AddOptions();
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
                new Infrastructure.Options.UploadOptions { Directory = Path.Combine(root, "vault") }));
            services.AddInfrastructureAdapterModules();
            services.AddScoped<Application.Imports.QueueImport>();
            using var provider = services.BuildServiceProvider();
            var watcher = new LocalFileProcessorService(NullLogger<LocalFileProcessorService>.Instance, config, provider);

            await provider.GetRequiredService<IVaultControl>().UnlockAsync("fake passphrase for tests");
            await watcher.ProcessFilesAsync(CancellationToken.None);
            await provider.GetRequiredService<IVaultControl>().UnlockAsync("fake passphrase for tests");
            await watcher.ProcessFilesAsync(CancellationToken.None);

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OfflineCPADbContext>();
            Assert.Empty(db.Transactions);
            var batch = Assert.Single(db.ImportBatches);
            Assert.Empty(db.StagedRows);
            Assert.NotNull(await scope.ServiceProvider.GetRequiredService<IDocumentVault>().OpenReadAsync(batch.DocumentId, batch.Extension));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
